using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Offers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;


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
            .Where(x => x.SupplierId == supplierId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new MyOfferDto(
                x.Id,
                x.Title,
                x.UnitPrice,
                x.Status,
                x.CreatedAt,
                x.ExpiresAt))
            .ToListAsync(cancellationToken);

        return offers;
    }
}