using Jomla.Application.Common.Extensions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Offers.DTOs;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Jomla.Application.Features.Offers.Queries.GetMyOffers;

public sealed class GetMyOffersQueryHandler(
    IAppDbContext db,
    IIdentityService identityService)
    : IRequestHandler<GetMyOffersQuery, MyOffersPagedResponse>
{
    public async Task<MyOffersPagedResponse> Handle(
        GetMyOffersQuery request,
        CancellationToken cancellationToken)
    {
        var supplierId = identityService.GetCurrentUserId();

        IQueryable<SupplierOffer> query = db.SupplierOffers
            .AsNoTracking()
            .Include(x => x.Batches)
            .Where(x => x.SupplierId == supplierId);

        query = query
            .ApplySearch(request.Search)
            .ApplyCategoryFilter(request.CategoryId)
            .ApplyStatusFilter(request.Status);

        var totalCount = await query.CountAsync(cancellationToken);

        query = query
            .ApplySorting(request.SortBy, request.Descending)
            .ApplyPagination(request.PageNumber, request.PageSize);

        var offers = await query
            .Select(x => new MyOfferDto(
                x.Id,
                x.Title,
                x.UnitPrice,
                x.DiscountPercentage,
                x.Status,
                x.TotalQuantityAvailable,
                x.Batches
                    .Where(b => b.Status == BatchStatus.Open)
                    .Sum(b => b.CurrentQuantity),
                x.BatchTargetQuantity,
                x.ImageUrls == null
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(x.ImageUrls, (JsonSerializerOptions?)null)!,
                x.Batches
                    .Where(b => b.Status == BatchStatus.Open)
                    .Select(b => (Guid?)b.Id)
                    .FirstOrDefault(),
                x.Batches
                    .Where(b => b.Status == BatchStatus.Open)
                    .Select(b => (int?)b.BatchNumber)
                    .FirstOrDefault(),
                x.CreatedAt,
                x.ExpiresAt
            ))
            .ToListAsync(cancellationToken);

        return new MyOffersPagedResponse
        {
            Items = offers,
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? totalCount,
            TotalCount = totalCount,
            TotalPages = request.PageNumber.HasValue
                ? (int)Math.Ceiling((double)totalCount / request.PageSize!.Value)
                : 1,

            ActiveOffersCount = await db.SupplierOffers.CountAsync(
                x => x.SupplierId == supplierId &&
                     x.Status == SupplierOfferStatus.Active,
                cancellationToken),

            ExpiredOffersCount = await db.SupplierOffers.CountAsync(
                x => x.SupplierId == supplierId &&
                     x.Status == SupplierOfferStatus.Expired,
                cancellationToken),

            PendingModerationCount = await db.SupplierOffers.CountAsync(
                x => x.SupplierId == supplierId &&
                     x.ModerationStatus == ModerationStatus.Pending,
                cancellationToken)
        };
    }
}