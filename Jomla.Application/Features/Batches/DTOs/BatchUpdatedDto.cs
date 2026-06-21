using Jomla.Domain;

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
        );
}
