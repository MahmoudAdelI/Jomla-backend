using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Jobs.Expiry;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.GroupRequests.Commands.NegotiateGroupRequestOffer
{
    public class NegotiateGroupRequestOfferCommandHandler(
        IAppDbContext db,
        INegotiationAgent negotiationAgent,
        IMediator mediator,
        IBackgroundJobDispatcher jobDispatcher
        ) : IRequestHandler<NegotiateGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly INegotiationAgent _negotiationAgent = negotiationAgent;
        private readonly IMediator _mediator = mediator;
        private readonly IBackgroundJobDispatcher _jobDispatcher = jobDispatcher;

        public  async Task Handle(NegotiateGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            // 1. load offer with what the agent needs
            var offer = await _db.GroupRequestOffers
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer is null) return;

            // 2. flip current round to Countered
            offer.Status = GroupRequestOfferStatus.Countered;
            await _db.SaveChangesAsync(cancellationToken);

            // 3. ask agent for next price
            var newPrice = await _negotiationAgent.GetNextPriceAsync(offer, request.categoryName);

            // 4. compute same duration as parent offer took
            var duration = offer.ExpiresAt - offer.CreatedAt;

            // 5. create child offer row
            var childOffer = new GroupRequestOffer
            {
                GroupRequestId = offer.GroupRequestId,
                SupplierId = offer.SupplierId,
                UnitPrice = offer.UnitPrice,
                MinUnitPrice = offer.MinUnitPrice,
                CurrentUnitPrice = newPrice,
                QuantityAvailable = offer.QuantityAvailable,
                MinFallbackQuantity = offer.MinFallbackQuantity,
                VariantAttributes = offer.VariantAttributes,
                RoundNumber = offer.RoundNumber + 1,
                ParentId = offer.Id,
                Status = GroupRequestOfferStatus.Open,
                ExpiresAt = DateTime.UtcNow.Add(duration)
            };

            // 6. schedule expiry job for child offer
            childOffer.JobId = _jobDispatcher.Schedule<IGroupRequestOfferExpiryJob>(job =>
                job.ExcuteAsync(childOffer.Id),
                new DateTimeOffset(childOffer.ExpiresAt, TimeSpan.Zero));

            _db.GroupRequestOffers.Add(childOffer);

            // 7. notify all active participants about the new offer
            var participantIds = offer.GroupRequest.Participants
            .Where(p => p.Status == GroupRequestParticipantStatus.Active)
            .Select(p => p.BuyerId);

            var notifications = participantIds.Select(buyerId => new Notification
            {
                UserId = buyerId,
                Type = NotificationType.GroupRequestOfferPlaced,
                Title = "New offer price available",
                Body = $"A new price of {newPrice:C} has been offered for {offer.GroupRequest.Title}.",
                EntityId = offer.GroupRequestId,
                EntityType = nameof(GroupRequest),
                CreatedAt = DateTime.UtcNow
            });
            _db.Notifications.AddRange(notifications);

            await _db.SaveChangesAsync(cancellationToken);

            foreach (var notification in notifications)
            {
                await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), cancellationToken);
            }
        }
    }
}
