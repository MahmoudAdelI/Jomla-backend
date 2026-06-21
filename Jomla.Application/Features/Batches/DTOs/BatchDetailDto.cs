using Jomla.Domain;

namespace Jomla.Application.Features.Batches.DTOs
{
    public sealed record BatchDetailDto(
        Guid Id,
        Guid OfferId,
        string OfferTitle,
        int BatchNumber,
        int TargetQuantity,
        int CurrentQuantity,
        int RemainingSlots,
        BatchStatus Status,
        decimal UnitPrice,
        decimal DiscountPercentage,
        decimal DiscountedPrice,
        string SupplierName,
        string CategoryName,
        DateTime CreatedAt,
        DateTime? CompletedAt,
        DateTime? ExpiresAt,
        IReadOnlyList<BatchParticipantDto> Participants
    );

    public sealed record BatchParticipantDto(
        Guid BuyerId,
        string BuyerName,
        int Quantity,
        BatchParticipantStatus Status,
        DateTime JoinedAt
    );
}
