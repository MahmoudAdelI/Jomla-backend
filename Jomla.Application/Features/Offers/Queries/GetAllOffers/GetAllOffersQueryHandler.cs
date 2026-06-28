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
            .Include(x => x.Batches)
                .ThenInclude(b => b.Participants);

        query = query
            .ApplySearch(request.Search)
            .ApplyCategoryFilter(request.CategoryId)
            .ApplyStatusFilter(request.Status);

        var totalCount = await query.CountAsync(cancellationToken);

        query = query
            .ApplySorting(request.SortBy, request.Descending)
            .ApplyPagination(request.PageNumber, request.PageSize);

        var dbOffers = await query.ToListAsync(cancellationToken);

        var offers = dbOffers.Select(x =>
        {
            var activeBatch = x.Batches.FirstOrDefault(b => b.Status == BatchStatus.Open);
            var committedUnits = activeBatch?.CurrentQuantity ?? 0;
            var buyerCount = activeBatch?.Participants?.Count(p => p.Status == BatchParticipantStatus.Active) ?? 0;

            return new OfferDto(
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
                activeBatch?.Id,
                committedUnits,
                x.BatchTargetQuantity,
                buyerCount,
                x.MinFallbackQuantity,
                x.Batches
                    .OrderByDescending(b => b.BatchNumber)
                    .Select(b => new OfferBatchDto(
                        b.Id,
                        b.BatchNumber,
                        b.TargetQuantity,
                        b.CurrentQuantity,
                        b.Status.ToString(),
                        b.CreatedAt,
                        b.CompletedAt
                    ))
                    .ToList()
            );
        }).ToList();

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