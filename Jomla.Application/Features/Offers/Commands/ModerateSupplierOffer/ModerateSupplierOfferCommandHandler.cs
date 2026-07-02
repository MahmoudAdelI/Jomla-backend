using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.Commands.CreateBatch;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Features.Offers.Queries.GetOfferById;
using Jomla.Application.Jobs.Expiry;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Jomla.Application.Features.Offers.Commands.ModerateSupplierOffer
{
    public class ModerateSupplierOfferCommandHandler(
        IAppDbContext db,
        IModerationAgent moderation,
        IMediator mediator,
        IBackgroundJobDispatcher jobDispatcher,
        IRealtimeService realtimeService) : IRequestHandler<ModerateSupplierOfferCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly IModerationAgent _moderation = moderation;
        private readonly IMediator _mediator = mediator;
        private readonly IBackgroundJobDispatcher _jobDispatcher = jobDispatcher;
        private readonly IRealtimeService _realtimeService = realtimeService;

        public async Task Handle(ModerateSupplierOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _db.SupplierOffers
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);
            if (offer is null) return;

            if (offer.ModerationStatus != ModerationStatus.Pending)
            {
                if (offer.ModerationStatus == ModerationStatus.Approved)
                {
                    var hasBatch = await _db.SupplierBatches.AnyAsync(b => b.OfferId == request.OfferId, cancellationToken);
                    if (!hasBatch)
                    {
                        await _mediator.Send(new CreateBatchCommand(offer.Id), cancellationToken);
                    }
                }
                return;
            }

            var imageUrls = string.IsNullOrWhiteSpace(offer.ImageUrls)
                ? []
                : JsonSerializer.Deserialize<List<string>>(offer.ImageUrls) ?? [];

            var moderationInput = new ModerationInput(offer.Title, offer.Description, imageUrls);
            var result = await _moderation.ModerateAsync(moderationInput, cancellationToken);

            offer.ModerationStatus = result.IsApproved
                ? ModerationStatus.Approved
                : ModerationStatus.Flagged;

            offer.ModerationReason = result.Reason;

            if (result.IsApproved)
            {
                // Flip offer status from PendingReview to Active
                offer.Status = SupplierOfferStatus.Active;

                if (offer.ExpiresAt.HasValue)
                {
                    offer.JobId = _jobDispatcher.Schedule<ISupplierOfferExpiryJob>(job =>
                        job.ExecuteAsync(offer.Id, CancellationToken.None),
                        new DateTimeOffset(offer.ExpiresAt.Value, TimeSpan.Zero));
                }
                else
                {
                    offer.JobId = string.Empty;
                }
            }
            else
            {
                // Flip offer status to Inactive if moderation fails
                offer.Status = SupplierOfferStatus.Inactive;
            }

            // Save notification to DB
            var notification = new Notification
            {
                UserId = offer.SupplierId,
                Type = result.IsApproved ? NotificationType.OfferApproved : NotificationType.OfferFlagged,
                Title = result.IsApproved ? "Your offer has been approved" : "Your offer has been flagged",
                Body = result.IsApproved
                    ? "Your offer is now live and visible to buyers."
                    : $"Your offer was flagged: {result.Reason}",
                EntityId = offer.Id,
                EntityType = nameof(SupplierOffer),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(cancellationToken);

            // If approved, open first batch
            if (result.IsApproved)
                await _mediator.Send(new CreateBatchCommand(offer.Id), cancellationToken);

            try
            {
                var offerDto = await _mediator.Send(new GetOfferByIdQuery(offer.Id), cancellationToken);
                if (offerDto != null)
                {
                    await _realtimeService.SendOfferStatusChangedAsync(offer.SupplierId, offerDto);
                }

                if (!result.IsApproved)
                {
                    await _realtimeService.SendFlaggedItemCreatedAsync("SupplierOffer", offer.Id);
                }
            }
            catch
            {
                // Non-blocking SignalR fallback
            }

            await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), cancellationToken);
        }
    }
}
