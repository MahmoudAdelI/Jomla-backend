using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.Commands.CreateBatch;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Offers.Commands.ActivateOffer
{
    public sealed record ActivateOfferCommand(Guid OfferId) : IRequest<bool>;

    public sealed class ActivateOfferCommandHandler(
        IAppDbContext db,
        IIdentityService identityService,
        IMediator mediator
        ) : IRequestHandler<ActivateOfferCommand, bool>
    {
        private readonly IAppDbContext _db = db;
        private readonly IIdentityService _identityService = identityService;
        private readonly IMediator _mediator = mediator;

        public async Task<bool> Handle(ActivateOfferCommand request, CancellationToken cancellationToken)
        {
            var supplierId = _identityService.GetCurrentUserId();

            var offer = await _db.SupplierOffers
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer is null)
                throw new NotFoundException(nameof(SupplierOffer), request.OfferId);

            if (offer.SupplierId != supplierId)
                throw new ForbiddenException("You are not the owner of this offer.");

            if (offer.Status == SupplierOfferStatus.Active)
                return true;

            if (offer.Status != SupplierOfferStatus.Inactive)
                throw new BadRequestException($"Only Inactive offers can be activated. Current status: {offer.Status}.");

            var minimumToOpen = offer.MinFallbackQuantity ?? offer.BatchTargetQuantity;
            if (offer.TotalQuantityAvailable < minimumToOpen)
                throw new BadRequestException($"Insufficient stock to activate. Required minimum: {minimumToOpen}. Available: {offer.TotalQuantityAvailable}.");

            if (offer.ExpiresAt.HasValue && offer.ExpiresAt.Value <= DateTime.UtcNow)
                throw new BadRequestException("Cannot activate an expired offer. Please update the expiry date first.");

            offer.Status = SupplierOfferStatus.Active;
            await _db.SaveChangesAsync(cancellationToken);

            // Open the initial batch for the reactivated offer
            await _mediator.Send(new CreateBatchCommand(offer.Id), cancellationToken);

            return true;
        }
    }
}
