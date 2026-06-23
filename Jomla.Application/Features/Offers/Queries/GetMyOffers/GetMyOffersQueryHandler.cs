using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Offers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Jomla.Domain;

namespace Jomla.Application.Features.Offers.Queries.GetMyOffers;

public sealed class GetMyOffersQueryHandler(IAppDbContext db,IIdentityService identityService): IRequestHandler<GetMyOffersQuery, List<MyOfferDto>>
{
    public async Task<List<MyOfferDto>> Handle(
        GetMyOffersQuery request,
        CancellationToken cancellationToken)
    {
        var supplierId = identityService.GetCurrentUserId();

        if (supplierId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var offers = await db.SupplierOffers
            .Include(x => x.Batches)
            .Where(x => x.SupplierId == supplierId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return offers.Select(x => {
            var activeBatch = x.Batches.FirstOrDefault(b => b.Status == BatchStatus.Open);
            var committedUnits = activeBatch?.CurrentQuantity ?? 0;
            var targetQuantity = activeBatch?.TargetQuantity ?? x.BatchTargetQuantity;
            var activeBatchId = activeBatch?.Id;
            var activeBatchNumber = activeBatch?.BatchNumber;

            return new MyOfferDto(
                x.Id,
                x.Title,
                x.UnitPrice,
                x.DiscountPercentage,
                x.Status,
                x.TotalQuantityAvailable,
                committedUnits,
                targetQuantity,
                activeBatchId,
                activeBatchNumber,
                x.CreatedAt,
                x.ExpiresAt
            );
        }).ToList();
    }
}