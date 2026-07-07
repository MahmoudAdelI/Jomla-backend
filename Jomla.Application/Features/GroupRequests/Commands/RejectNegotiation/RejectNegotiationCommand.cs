using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Commands.FailGroupRequestOffer;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.RejectNegotiation
{
    public record RejectNegotiationCommand(Guid OfferId, Guid SupplierId) : IRequest<bool>;

    public class RejectNegotiationCommandHandler(
        IAppDbContext db,
        IMediator mediator
        ) : IRequestHandler<RejectNegotiationCommand, bool>
    {
        private readonly IAppDbContext _db = db;
        private readonly IMediator _mediator = mediator;

        public async Task<bool> Handle(RejectNegotiationCommand request, CancellationToken cancellationToken)
        {
            var offer = await _db.GroupRequestOffers
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer == null)
                throw new NotFoundException(nameof(GroupRequestOffer), request.OfferId);

            if (offer.SupplierId != request.SupplierId)
                throw new ForbiddenException("You are not authorized to reject this negotiation proposal.");

            if (offer.Status != GroupRequestOfferStatus.PendingSupplierApproval)
                throw new ConflictException($"Offer status is {offer.Status}, cannot reject.");

            // Delegate to FailGroupRequestOfferCommand which handles:
            // - setting status to Expired
            // - cancelling Stripe payment holds
            // - saving to DB
            // - sending SignalR updates
            await _mediator.Send(new FailGroupRequestOfferCommand(offer.Id), cancellationToken);

            return true;
        }
    }
}
