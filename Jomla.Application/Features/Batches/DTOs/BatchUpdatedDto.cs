using Jomla.Domain;
using Jomla.Domain.Entities;
using System;

namespace Jomla.Application.Features.Batches.DTOs
{
    public sealed record BatchUpdatedDto(
        Guid OfferId,
        Guid BatchId,
        int BatchNumber,
        int CurrentQuantity,
        int TargetQuantity,
        int RemainingSlots,
        BatchStatus Status,
        BatchUpdatedDto? NewBatch
        )
    {
        public static BatchUpdatedDto MapFrom(SupplierBatch batch, BatchUpdatedDto? newBatch = null)
        {
            return new BatchUpdatedDto(
                batch.OfferId,
                batch.Id,
                batch.BatchNumber,
                batch.CurrentQuantity,
                batch.TargetQuantity,
                Math.Max(0, batch.TargetQuantity - batch.CurrentQuantity),
                batch.Status,
                newBatch
            );
        }
    }
}
