using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Commands.CompleteGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.CancelGroupRequestOffer;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.GroupRequests.Commands.ExpireGroupRequestOffer
{
    public class ExpireGroupRequestOfferCommandHandler(IAppDbContext db, ISender sender)
        : IRequestHandler<ExpireGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly ISender _sender = sender;

        public async Task Handle(ExpireGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _db.GroupRequestOffers
                .Include(o => o.Responses)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer is null || offer.Status != GroupRequestOfferStatus.Open) return;

            var acceptedBuyerIds = offer.Responses
                .Where(r => r.Response == BuyerOfferResponseType.Accepted)
                .Select(r => r.BuyerId)
                .ToHashSet();

            var acceptedQuantity = offer.GroupRequest.Participants
                .Where(p =>
                    acceptedBuyerIds.Contains(p.BuyerId) &&
                    p.Status == GroupRequestParticipantStatus.Active)
                .Sum(r => r.Quantity);

            var shouldCapture = offer.MinFallbackQuantity.HasValue &&
                acceptedQuantity >= offer.MinFallbackQuantity.Value;
            if (shouldCapture)
            {
                await _sender.Send(new CompleteGroupRequestOfferCommand(offer.Id), cancellationToken);
            }
            else
            {
                await _sender.Send(new CancelGroupRequestOfferCommand(offer.Id), cancellationToken);
            }

            offer.Status = GroupRequestOfferStatus.Expired;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
