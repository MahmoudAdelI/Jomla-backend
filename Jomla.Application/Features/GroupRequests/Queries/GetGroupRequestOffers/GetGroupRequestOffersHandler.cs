using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOffers;

public sealed class GetGroupRequestOffersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetGroupRequestOffersQuery, List<BuyerGroupRequestOfferDto>>
{
    public async Task<List<BuyerGroupRequestOfferDto>> Handle(
        GetGroupRequestOffersQuery request,
        CancellationToken cancellationToken)
    {
        var exists = await db.GroupRequests
            .AnyAsync(
                x => x.Id == request.GroupRequestId,
                cancellationToken);

        if (!exists)
        {
            throw new NotFoundException(
                nameof(GroupRequest),
                request.GroupRequestId);
        }

        IQueryable<GroupRequestOffer> query = db.GroupRequestOffers
            .AsNoTracking()
            .Where(x => x.GroupRequestId == request.GroupRequestId);

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }
        else
        {
            query = query.Where(x => x.Status == GroupRequestOfferStatus.Open);
        }

        var offers = await query
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
            )).ToListAsync(cancellationToken);

        return offers;
    
}
}