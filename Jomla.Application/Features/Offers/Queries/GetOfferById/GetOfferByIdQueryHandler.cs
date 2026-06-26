using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Offers.DTOs;
using Jomla.Application.Features.Offers.Queries.GetAllOffers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jomla.Domain;

namespace Jomla.Application.Features.Offers.Queries.GetOfferById;

public sealed class GetOfferByIdQueryHandler: IRequestHandler<GetOfferByIdQuery, OfferDto>
{
    private readonly IAppDbContext _db;

    public GetOfferByIdQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<OfferDto> Handle(
        GetOfferByIdQuery request,
        CancellationToken cancellationToken)
    {
        var offer = await _db.SupplierOffers
            .Include(x => x.Category)
            .Include(x => x.Supplier)
            .Include(x => x.Batches)
                .ThenInclude(b => b.Participants)
            .FirstOrDefaultAsync(
                x => x.Id == request.Id,
                cancellationToken);

        if (offer is null)
            throw new KeyNotFoundException("Offer not found.");

        var activeBatch = offer.Batches.FirstOrDefault(b => b.Status == BatchStatus.Open);
        var committedUnits = activeBatch?.CurrentQuantity ?? 0;
        var targetQuantity = activeBatch?.TargetQuantity ?? 0;
        var buyerCount = activeBatch?.Participants.Count(p => p.Status == BatchParticipantStatus.Active) ?? 0;

        return new OfferDto(
            offer.Id,
            offer.Title,
            offer.Description,
            offer.UnitPrice,
            offer.DiscountPercentage,
            offer.Category.Name,
            $"{offer.Supplier.FirstName} {offer.Supplier.LastName}",
            string.IsNullOrWhiteSpace(offer.ImageUrls)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(offer.ImageUrls)!,
            offer.CreatedAt,
            offer.ExpiresAt,
            activeBatch?.Id,
            committedUnits,
            targetQuantity,
            buyerCount,
            offer.MinFallbackQuantity
        );
    }
}
