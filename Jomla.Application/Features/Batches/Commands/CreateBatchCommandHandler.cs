using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Batches.Commands
{
    public class CreateBatchCommandHandler : IRequestHandler<CreateBatchCommand, CreateBatchResponse>
    {
        private readonly IAppDbContext _context;

        public CreateBatchCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<CreateBatchResponse> Handle(CreateBatchCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Step 1: Fetch offer
                var offer = await _context.SupplierOffers
                    .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

                if (offer == null)
                {
                    return new CreateBatchResponse
                    {
                        Success = false,
                        Error = "Offer not found."
                    };
                }

                // Step 2: Validate offer is Active
                if (offer.Status != SupplierOfferStatus.Active)
                {
                    return new CreateBatchResponse
                    {
                        Success = false,
                        Error = $"Offer status is {offer.Status}. Cannot open batch."
                    };
                }

                // Step 3: Validate no other Open batch exists
                var existingOpenBatch = await _context.SupplierBatches
                    .Where(b => b.OfferId == request.OfferId && b.Status == BatchStatus.Open)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingOpenBatch != null)
                {
                    return new CreateBatchResponse
                    {
                        Success = false,
                        Error = "An open batch already exists for this offer. Complete it first."
                    };
                }

                // Step 4: Calculate remaining quantity
                var usedQuantity = await _context.SupplierBatches
                    .Where(b => b.OfferId == request.OfferId)
                    .SumAsync(b => (int?)b.TargetQuantity, cancellationToken) ?? 0;

                var remainingQuantity = offer.TotalQuantityAvailable - usedQuantity;

                int targetQuantity = Math.Min(
                    offer.BatchTargetQuantity,
                    remainingQuantity);

                if (targetQuantity <= 0)
                {
                    return new CreateBatchResponse
                    {
                        Success = false,
                        Error = "No quantity available to open batch."
                    };
                }

                // Step 5: Generate Batch Number (بدل ما ييجي من request)
                var lastBatchNumber = await _context.SupplierBatches
                    .Where(b => b.OfferId == request.OfferId)
                    .MaxAsync(b => (int?)b.BatchNumber, cancellationToken) ?? 0;

                var batch = new SupplierBatch
                {
                    Id = Guid.NewGuid(),
                    OfferId = request.OfferId,
                    BatchNumber = lastBatchNumber + 1,
                    TargetQuantity = targetQuantity,
                    CurrentQuantity = 0,
                    Status = BatchStatus.Open,
                    CreatedAt = DateTime.UtcNow
                };

                // Step 6: Add
                _context.SupplierBatches.Add(batch);

                // Step 7: Save
                await _context.SaveChangesAsync(cancellationToken);

                // Step 8: Response
                return new CreateBatchResponse
                {
                    Success = true,
                    BatchId = batch.Id,
                    TargetQuantity = batch.TargetQuantity,
                    BatchNumber = batch.BatchNumber,
                    Message = $"Batch #{batch.BatchNumber} opened successfully with target quantity {batch.TargetQuantity}."
                };
            }
            catch (Exception ex)
            {
                return new CreateBatchResponse
                {
                    Success = false,
                    Error = $"Failed to create batch: {ex.Message}"
                };
            }
        }
    }
}