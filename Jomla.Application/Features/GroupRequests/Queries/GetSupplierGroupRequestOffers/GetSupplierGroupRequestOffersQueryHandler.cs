using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Queries.GetSupplierGroupRequestOffers;

public sealed class GetSupplierGroupRequestOffersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetSupplierGroupRequestOffersQuery, PagedResult<SupplierGroupRequestOfferDto>>
{
    public async Task<PagedResult<SupplierGroupRequestOfferDto>> Handle(
        GetSupplierGroupRequestOffersQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.GroupRequestOffers
            .AsNoTracking()
            .Where(x => x.SupplierId == request.SupplierId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SupplierGroupRequestOfferDto(
                x.Id,
                x.GroupRequestId,
                x.GroupRequest.Title,
                x.CurrentUnitPrice,
                x.QuantityAvailable,
                x.Status.ToString(),
                x.CreatedAt,
                x.ExpiresAt,
                x.VariantAttributes
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierGroupRequestOfferDto>(items, totalCount, request.Page, request.PageSize);
    }
}
