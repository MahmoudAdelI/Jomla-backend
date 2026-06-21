using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Batches.Commands.CreateBatch
{
    public class CreateBatchCommandHandler(IAppDbContext db) : IRequestHandler<CreateBatchCommand, Guid?>
    {
        private readonly IAppDbContext _db = db;

        public async Task<Guid?> Handle(CreateBatchCommand request, CancellationToken cancellationToken)
        {
            // Load offer with its batches to avoid extra queries downstream
            var offer = await _db.SupplierOffers
                .Include(o => o.Batches)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);
            // Only active offers can have new batches opened
            if (offer is null || offer.Status != SupplierOfferStatus.Active) return null;

            // Defensive guard — should never be true given our callers, but prevents double-opening
            var hasOpenBatch = offer.Batches.Any(b => b.Status == BatchStatus.Open);
            if (hasOpenBatch) return null;

            // Minimum stock required to open a new batch:
            // if supplier set a fallback threshold use it, otherwise require a full batch worth of stock
            var minimumToOpen = offer.MinFallbackQuantity ?? offer.BatchTargetQuantity;
            if (offer.TotalQuantityAvailable < minimumToOpen)
            {
                // Remaining stock is too low to be worth opening another batch
                offer.Status = SupplierOfferStatus.Inactive;
                await _db.SaveChangesAsync(cancellationToken);
                return null;
            }

            // Last batch may be smaller than BatchTargetQuantity if remaining stock is less
            var targetQuantity = Math.Min(offer.TotalQuantityAvailable, offer.BatchTargetQuantity);

            var lastBatchNumber = offer.Batches.Any() ? offer.Batches.Max(b => b.BatchNumber) : 0;

            var batch = new SupplierBatch
            {
                OfferId = offer.Id,
                BatchNumber = lastBatchNumber + 1,
                TargetQuantity = targetQuantity,
                Status = BatchStatus.Open,
                CurrentQuantity = 0
            };

            _db.SupplierBatches.Add(batch);
            await _db.SaveChangesAsync(cancellationToken);

            return batch.Id;
        }
    }
}