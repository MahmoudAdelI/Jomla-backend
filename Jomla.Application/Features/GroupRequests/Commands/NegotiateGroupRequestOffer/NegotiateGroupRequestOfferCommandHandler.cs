using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Jobs.Expiry;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Jomla.Application.Features.GroupRequests.Queries;

namespace Jomla.Application.Features.GroupRequests.Commands.NegotiateGroupRequestOffer
{
    public class NegotiateGroupRequestOfferCommandHandler(
        IAppDbContext db,
        INegotiationAgent negotiationAgent,
        IMediator mediator,
        IBackgroundJobDispatcher jobDispatcher,
        IRealtimeService realtimeService
        ) : IRequestHandler<NegotiateGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly INegotiationAgent _negotiationAgent = negotiationAgent;
        private readonly IMediator _mediator = mediator;
        private readonly IBackgroundJobDispatcher _jobDispatcher = jobDispatcher;
        private readonly IRealtimeService _realtimeService = realtimeService;

        public  async Task Handle(NegotiateGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            // 1. load offer with what the agent needs
            var offer = await _db.GroupRequestOffers
                .Include(o => o.Responses)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer is null) return;

            // 2. flip current round to Countered
            offer.Status = GroupRequestOfferStatus.Countered;

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
                Status = GroupRequestOfferStatus.PendingSupplierApproval,
                AcceptedQuantity = offer.AcceptedQuantity,
                ExpiresAt = DateTime.UtcNow.Add(duration)
            };

            // 5b. copy active acceptances from parent to child, and seal the parent responses
            var activeAcceptances = offer.Responses.Where(r => r.Response == BuyerOfferResponseType.Accepted).ToList();
            foreach (var parentResponse in activeAcceptances)
            {
                var childResponse = new BuyerOfferResponse
                {
                    Offer = childOffer,
                    BuyerId = parentResponse.BuyerId,
                    Response = BuyerOfferResponseType.Accepted,
                    StripePaymentIntentId = parentResponse.StripePaymentIntentId,
                    RespondedAt = parentResponse.RespondedAt,
                    ShippingAddress = parentResponse.ShippingAddress,
                    PhoneNumber = parentResponse.PhoneNumber
                };
                _db.BuyerOfferResponses.Add(childResponse);

                // Mark the parent response as superseded so it is never treated as a live
                // Accepted response again (prevents double-counting and double-cancellation).
                parentResponse.Response = BuyerOfferResponseType.MovedToNextRound;
            }

            _db.GroupRequestOffers.Add(childOffer);

            // 5c. log the negotiation step
            var log = new NegotiationLog
            {
                Offer = childOffer,
                PreviousPrice = offer.CurrentUnitPrice,
                NewPrice = newPrice,
                ReasoningSummary = $"AI Agent countered with a new price of {newPrice:C}.",
                ActedAt = DateTime.UtcNow
            };
            _db.NegotiationLogs.Add(log);

            // Save changes first to obtain the SQL Server-generated sequential GUID for childOffer.Id
            await _db.SaveChangesAsync(cancellationToken);

            var notifications = new List<Notification>();

            // 7b. notify the supplier about the AI agent counter-offer (using the GroupRequest ID)
            var supplierNotification = new Notification
            {
                UserId = offer.SupplierId,
                Type = NotificationType.NegotiationPendingApproval,
                Title = "New AI Price Counter Proposed",
                Body = $"Your AI pricing agent proposed a counter-offer of {newPrice:C} for {offer.GroupRequest.Title}. Please approve or reject it.",
                EntityId = offer.GroupRequestId,
                EntityType = nameof(GroupRequest),
                CreatedAt = DateTime.UtcNow
            };
            notifications.Add(supplierNotification);

            _db.Notifications.AddRange(notifications);

            // Save the new notifications
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var detail = await _mediator.Send(new GetGroupRequestDetailQuery(offer.GroupRequestId), cancellationToken);
                if (detail != null)
                {
                    await _realtimeService.SendGroupRequestUpdatedAsync(offer.GroupRequestId, detail);
                }
            }
            catch
            {
                // Non-blocking SignalR fallback
            }

            foreach (var notification in notifications)
            {
                await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), cancellationToken);
            }
        }
    }
}
