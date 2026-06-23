using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.Commands.CreateBatch;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Jobs.Agents;
using Jomla.Application.Jobs.Expiry;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Infrastructure.Persistance;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Jomla.Infrastructure.Jobs.Agents
{
    public class ModerateSupplierOfferJob(
        IDbContextFactory<AppDbContext> contextFactory,
        IModerationService moderation,
        IMediator mediator,
        IBackgroundJobDispatcher jobDispatcher) : IModerateSupplierOfferJob
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;
        private readonly IModerationService _moderation = moderation;
        private readonly IMediator _mediator = mediator;
        private readonly IBackgroundJobDispatcher _jobDispatcher = jobDispatcher;

        public async Task ExecuteAsync(Guid offerId, CancellationToken ct)
        {
            await using var db = await _contextFactory.CreateDbContextAsync(ct);

            var offer = await db.SupplierOffers
                .FirstOrDefaultAsync(o => o.Id == offerId, ct);
            if (offer is null) return;

            var imageUrls = string.IsNullOrWhiteSpace(offer.ImageUrls)
                ? []
                : JsonSerializer.Deserialize<List<string>>(offer.ImageUrls) ?? [];

            var moderationInput = new ModerationInput(offer.Title, offer.Description, imageUrls);
            var result = await _moderation.ModerateAsync(moderationInput, ct);

            offer.ModerationStatus = result.IsApproved
                ? ModerationStatus.Approved
                : ModerationStatus.Flagged;

            offer.ModerationReason = result.Reason;

            if (result.IsApproved)
            {
                // Flip offer status from PendingReview to Active
                offer.Status = SupplierOfferStatus.Active;

                // Schedule expiry job if offer has an expiry date
                if (offer.ExpiresAt.HasValue)
                {
                    var jobId = _jobDispatcher.Schedule<ISupplierOfferExpiryJob>(job=>
                        job.ExecuteAsync(offer.Id, CancellationToken.None),
                        new DateTimeOffset(offer.ExpiresAt.Value, TimeSpan.Zero));

                    offer.JobId = jobId;
                }
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
                IsRead = false
            };

            db.Notifications.Add(notification);
            await db.SaveChangesAsync(ct);

            // If approved, open first batch (stub — Sprint 2)
            if (result.IsApproved)
                await _mediator.Send(new CreateBatchCommand(offer.Id), ct);

            await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), ct);
        }
    }
}
