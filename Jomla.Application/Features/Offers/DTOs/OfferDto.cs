using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Offers.DTOs
{
    public sealed record OfferDto(
        Guid Id,
        string Title,
        string? Description,
        decimal UnitPrice,
        decimal DiscountPercentage,
        string CategoryName,
        string SupplierName,
        List<string> Images,
        DateTime CreatedAt,
        DateTime? ExpiresAt,
        Guid? ActiveBatchId,
        int CommittedUnits,
        int HubTargetQuantity,
        int BuyerCount,
        int? MinFallbackQuantity,
        List<OfferBatchDto> Batches
    );

    public sealed record OfferBatchDto(
        Guid Id,
        int BatchNumber,
        int TargetQuantity,
        int CurrentQuantity,
        string Status,
        DateTime CreatedAt,
        DateTime? CompletedAt
    );
}
