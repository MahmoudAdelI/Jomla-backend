using Jomla.Application.Common.BaseClass;
using Jomla.Application.Common.Extensions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Offers.DTOs;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Jomla.Application.Features.Offers.Queries.GetAllOffers;

public sealed class GetAllOffersQueryHandler(
    IAppDbContext db)
    : IRequestHandler<GetAllOffersQuery, GetAllOffersPagedResponse>
{
    public async Task<GetAllOffersPagedResponse> Handle(
        GetAllOffersQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<SupplierOffer> query = db.SupplierOffers
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Supplier)
            .Include(x => x.Batches);

        query = query
            .ApplySearch(request.Search)
            .ApplyCategoryFilter(request.CategoryId)
            .ApplyStatusFilter(request.Status);

        var totalCount = await query.CountAsync(cancellationToken);

        query = query
            .ApplySorting(request.SortBy, request.Descending)
            .ApplyPagination(request.PageNumber, request.PageSize);

        var offers = await query
            .Select(x => new OfferDto(
                x.Id,
                x.Title,
                x.Description,
                x.UnitPrice,
                x.DiscountPercentage,
                x.Category.Name,
                x.Supplier.FirstName + " " + x.Supplier.LastName,
                x.ImageUrls == null
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(x.ImageUrls, (JsonSerializerOptions?)null)!,
                x.CreatedAt,
                x.ExpiresAt,
                x.Batches
                    .Where(b => b.Status == BatchStatus.Open)
                    .Select(b => (Guid?)b.Id)
                    .FirstOrDefault(),
                x.Batches.Sum(b => b.CurrentQuantity),
                x.BatchTargetQuantity,
                x.Batches.Count(b => b.Status == BatchStatus.Open)
            ))
            .ToListAsync(cancellationToken);

        return new GetAllOffersPagedResponse
        {
            Items = offers,
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? totalCount,
            TotalCount = totalCount,
            TotalPages = request.PageNumber.HasValue
                ? (int)Math.Ceiling((double)totalCount / request.PageSize!.Value)
                : 1,

            ActiveOffersCount = await db.SupplierOffers.CountAsync(
                x => x.Status == SupplierOfferStatus.Active,
                cancellationToken),

            ExpiredOffersCount = await db.SupplierOffers.CountAsync(
                x => x.Status == SupplierOfferStatus.Expired,
                cancellationToken),

            PendingModerationCount = await db.SupplierOffers.CountAsync(
                x => x.ModerationStatus == ModerationStatus.Pending,
                cancellationToken)
        };
    }
}