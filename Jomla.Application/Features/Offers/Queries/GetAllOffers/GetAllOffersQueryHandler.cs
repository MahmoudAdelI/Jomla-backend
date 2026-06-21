using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Offers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Offers.Queries.GetAllOffers;

public sealed class GetAllOffersQueryHandler: IRequestHandler<GetAllOffersQuery, List<OfferDto>>
{
    private readonly IAppDbContext _db;

    public GetAllOffersQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<OfferDto>> Handle(
        GetAllOffersQuery request,
        CancellationToken cancellationToken)
    {
        var offers = await _db.SupplierOffers
            .Include(x => x.Category)
            .Include(x => x.Supplier)
            .ToListAsync(cancellationToken);

        return offers.Select(x => new OfferDto(
            x.Id,
            x.Title,
            x.Description,
            x.UnitPrice,
            x.DiscountPercentage,
            x.Category.Name,
            $"{x.Supplier.FirstName} {x.Supplier.LastName}",
            string.IsNullOrWhiteSpace(x.ImageUrls)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(x.ImageUrls)!,
            x.CreatedAt,
            x.ExpiresAt
        )).ToList();
    }
}