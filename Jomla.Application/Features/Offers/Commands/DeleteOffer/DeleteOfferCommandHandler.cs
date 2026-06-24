using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Offers.Commands.DeleteOffer;

public sealed class DeleteOfferCommandHandler: IRequestHandler<DeleteOfferCommand, bool>
{
    private readonly IAppDbContext _db;
    private readonly IIdentityService _identityService;

    public DeleteOfferCommandHandler(
        IAppDbContext db,
        IIdentityService identityService)
    {
        _db = db;
        _identityService = identityService;
    }

    public async Task<bool> Handle(
        DeleteOfferCommand request,
        CancellationToken cancellationToken)
    {
        var supplierId = _identityService.GetCurrentUserId();

        var offer = await _db.SupplierOffers
            .Include(x => x.Batches)
            .FirstOrDefaultAsync(
                x => x.Id == request.Id,
                cancellationToken);

        if (offer is null)
            throw new KeyNotFoundException("Offer not found.");

        if (offer.SupplierId != supplierId)
            throw new UnauthorizedAccessException();

        if (offer.Batches.Any(b => b.Status == BatchStatus.Open || b.Status == BatchStatus.Completed))
        {
            throw new InvalidOperationException("Cannot delete an offer with active (open or completed) batches.");
        }

        _db.SupplierOffers.Remove(offer);

        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}