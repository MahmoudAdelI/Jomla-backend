using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Commands.ExpireGroupRequestOffer;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.TriggerNegotiation
{
    public record TriggerNegotiationCommand(Guid OfferId, Guid SupplierId) : IRequest<bool>;

    public class TriggerNegotiationCommandHandler(
        IAppDbContext db,
        IBackgroundJobDispatcher jobDispatcher,
        ISender sender
        ) : IRequestHandler<TriggerNegotiationCommand, bool>
    {
        private readonly IAppDbContext _db = db;
        private readonly IBackgroundJobDispatcher _jobDispatcher = jobDispatcher;
        private readonly ISender _sender = sender;

        public async Task<bool> Handle(TriggerNegotiationCommand request, CancellationToken cancellationToken)
        {
            var offer = await _db.GroupRequestOffers
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer == null)
                throw new NotFoundException(nameof(GroupRequestOffer), request.OfferId);

            if (offer.SupplierId != request.SupplierId)
                throw new ForbiddenException("You are not authorized to trigger negotiation for this offer.");

            if (offer.Status != GroupRequestOfferStatus.Open)
                throw new ConflictException($"Offer status is {offer.Status}, only Open offers can trigger negotiation.");

            // 1. Delete scheduled Hangfire expiry job if it exists
            if (!string.IsNullOrEmpty(offer.JobId))
            {
                try
                {
                    _jobDispatcher.Delete(offer.JobId);
                }
                catch
                {
                    // Ignore background job cancellation failure if it already ran or was deleted
                }
            }

            // 2. Delegate directly to the existing ExpireGroupRequestOfferCommand
            // which handles Path A (fill), Path B (fail), or Path C (negotiate/counter).
            await _sender.Send(new ExpireGroupRequestOfferCommand(offer.Id), cancellationToken);

            return true;
        }
    }
}
