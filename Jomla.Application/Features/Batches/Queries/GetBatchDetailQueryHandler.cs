using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.DTOs;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Batches.Queries
{
    public class GetBatchDetailQueryHandler : IRequestHandler<GetBatchDetailQuery, BatchDetailDto>
    {
        private readonly IAppDbContext _context;

        public GetBatchDetailQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<BatchDetailDto> Handle(GetBatchDetailQuery request, CancellationToken cancellationToken)
        {
            var batch = await _context.SupplierBatches
                .Include(b => b.Offer)
                    .ThenInclude(o => o.Supplier)
                .Include(b => b.Offer)
                    .ThenInclude(o => o.Category)
                .Include(b => b.Participants)
                    .ThenInclude(p => p.Buyer)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

            if (batch is null)
                throw new NotFoundException(nameof(SupplierBatch), request.BatchId);

            var offer = batch.Offer;
            var discountedPrice = offer.UnitPrice * (1 - offer.DiscountPercentage / 100m);

            var participants = batch.Participants
                .Where(p => p.Status == Domain.BatchParticipantStatus.Active)
                .Select(p => new BatchParticipantDto(
                    p.BuyerId,
                    $"{p.Buyer.FirstName} {p.Buyer.LastName}",
                    p.Quantity,
                    p.Status,
                    p.JoinedAt
                ))
                .ToList();

            return new BatchDetailDto(
                batch.Id,
                offer.Id,
                offer.Title,
                batch.BatchNumber,
                batch.TargetQuantity,
                batch.CurrentQuantity,
                batch.TargetQuantity - batch.CurrentQuantity,
                batch.Status,
                offer.UnitPrice,
                offer.DiscountPercentage,
                discountedPrice,
                $"{offer.Supplier.FirstName} {offer.Supplier.LastName}",
                offer.Category.Name,
                batch.CreatedAt,
                batch.CompletedAt,
                offer.ExpiresAt,
                participants
            );
        }
    }
}
