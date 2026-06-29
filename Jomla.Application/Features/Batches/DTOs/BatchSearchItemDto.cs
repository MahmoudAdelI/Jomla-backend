using Jomla.Domain;
using System;

namespace Jomla.Application.Features.Batches.DTOs
{
    public sealed record BatchSearchItemDto(
        Guid Id,
        Guid OfferId,
        string OfferTitle,
        int BatchNumber,
        int TargetQuantity,
        int CurrentQuantity,
        int RemainingSlots,
        string Status,
        decimal UnitPrice,
        decimal DiscountPercentage,
        decimal DiscountedPrice,
        string SupplierName,
        string CategoryName,
        DateTime CreatedAt,
        DateTime? CompletedAt,
        DateTime? ExpiresAt
    );
}
