using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.DTOs;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Queries.SearchBatches
{
    public sealed class SearchBatchesQueryHandler : IRequestHandler<SearchBatchesQuery, PagedBatchesResult>
    {
        private readonly IAppDbContext _context;

        public SearchBatchesQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedBatchesResult> Handle(SearchBatchesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.SupplierBatches
                .Include(b => b.Offer)
                    .ThenInclude(o => o.Supplier)
                .Include(b => b.Offer)
                    .ThenInclude(o => o.Category)
                .AsNoTracking();

            // Filter by SearchTerm
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(b => 
                    b.Offer.Title.ToLower().Contains(term) ||
                    b.Offer.Supplier.FirstName.ToLower().Contains(term) ||
                    b.Offer.Supplier.LastName.ToLower().Contains(term) ||
                    b.Offer.Category.Name.ToLower().Contains(term)
                );
            }

            // Filter by Status
            if (!string.IsNullOrWhiteSpace(request.Status) && request.Status.ToLower() != "all")
            {
                if (Enum.TryParse<BatchStatus>(request.Status, true, out var statusEnum))
                {
                    query = query.Where(b => b.Status == statusEnum);
                }
            }

            // Count total
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and sorting
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(b => new BatchSearchItemDto(
                    b.Id,
                    b.OfferId,
                    b.Offer.Title,
                    b.BatchNumber,
                    b.TargetQuantity,
                    b.CurrentQuantity,
                    b.TargetQuantity - b.CurrentQuantity,
                    b.Status.ToString(),
                    b.Offer.UnitPrice,
                    b.Offer.DiscountPercentage,
                    b.Offer.UnitPrice * (1 - b.Offer.DiscountPercentage / 100m),
                    $"{b.Offer.Supplier.FirstName} {b.Offer.Supplier.LastName}",
                    b.Offer.Category.Name,
                    b.CreatedAt,
                    b.CompletedAt,
                    b.Offer.ExpiresAt
                ))
                .ToListAsync(cancellationToken);

            return new PagedBatchesResult(items, totalCount, request.Page, request.PageSize);
        }
    }
}
