using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Commands.FailGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.NegotiateGroupRequestOffer;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Application.Jobs.Sync;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.GroupRequests.Commands.ExpireGroupRequestOffer
{
    public class ExpireGroupRequestOfferCommandHandler(
        IAppDbContext db, 
        ISender sender,
        IBackgroundJobDispatcher jobDispatcher
        )
        : IRequestHandler<ExpireGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly ISender _sender = sender;
        private readonly IBackgroundJobDispatcher _jobDispatcher = jobDispatcher;

        public async Task Handle(ExpireGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _db.GroupRequestOffers
                .Include(o => o.Responses)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Category)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer is null || offer.Status != GroupRequestOfferStatus.Open) return;

            var categoryName = offer.GroupRequest.Category.Name;
            var totalParticipants = offer.GroupRequest.Participants
                .Count(p => p.Status == GroupRequestParticipantStatus.Active);

            var acceptedBuyerIds = offer.Responses
                .Where(r => r.Response == BuyerOfferResponseType.Accepted)
                .Select(r => r.BuyerId)
                .ToHashSet();

            var acceptedQuantity = offer.GroupRequest.Participants
                .Where(p => acceptedBuyerIds.Contains(p.BuyerId))
                .Sum(r => r.Quantity);

            // path A — fallback quantity met → offload capture to a dedicated background job
            // The indexer is enqueued from inside the fill job, after the offer is Accepted.
            var shouldCapture = offer.MinFallbackQuantity.HasValue &&
                acceptedQuantity >= offer.MinFallbackQuantity.Value;
            if (shouldCapture)
            {
                _jobDispatcher.Enqueue<IGroupRequestOfferFillJob>(j => j.ExecuteAsync(offer.Id));
                return;
            }
         

            // path B — no room to negotiate (already at floor)
            var floor = offer.MinUnitPrice ?? offer.UnitPrice;
            if (offer.CurrentUnitPrice <= floor)
            {
                await _sender.Send(new FailGroupRequestOfferCommand(offer.Id), cancellationToken);
                offer.Status = GroupRequestOfferStatus.Expired;
                await _db.SaveChangesAsync(cancellationToken);
                _jobDispatcher.Enqueue<INegotiationRoundIndexJob>(j => j.ExcuteAsync(offer.Id));
                return;
            }

            // path C — negotiation → counter current round, create child offer
            await _sender.Send(new NegotiateGroupRequestOfferCommand(offer.Id, categoryName), cancellationToken);
            _jobDispatcher.Enqueue<INegotiationRoundIndexJob>(j => j.ExcuteAsync(offer.Id));

        }
    }
}
