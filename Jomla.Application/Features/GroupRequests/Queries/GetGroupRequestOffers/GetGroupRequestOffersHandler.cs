using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOffers;

public sealed class GetGroupRequestOffersHandler(IAppDbContext db)
    : IRequestHandler<GetGroupRequestOffersQuery, List<BuyerGroupRequestOfferDto>>
{
    public async Task<List<BuyerGroupRequestOfferDto>> Handle(
        GetGroupRequestOffersQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.GroupRequestOffers
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Where(x => x.GroupRequestId == request.GroupRequestId);

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }
        else
        {
            query = query.Where(x => x.Status == GroupRequestOfferStatus.Open);
        }

        return await query
            .OrderBy(x => x.CurrentUnitPrice)
            .Select(x => new BuyerGroupRequestOfferDto(
                x.Id,
                x.SupplierId,
                x.Supplier.FirstName + " " + x.Supplier.LastName,
                x.CurrentUnitPrice,
                x.QuantityAvailable,
                x.AcceptedQuantity,
                x.Status,
                x.RoundNumber,
                x.ExpiresAt,
                x.VariantAttributes
            ))
            .ToListAsync(cancellationToken);
    }
}