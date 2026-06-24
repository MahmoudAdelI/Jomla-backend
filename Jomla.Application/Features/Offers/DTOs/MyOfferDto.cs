using Jomla.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Offers.DTOs
{
    public sealed record MyOfferDto(
        Guid Id,
        string Title,
        decimal UnitPrice,
        decimal DiscountPercentage,
        SupplierOfferStatus Status,
        int TotalQuantityAvailable,
        int CommittedUnits,
        int BatchTargetQuantity,
        Guid? ActiveBatchId,
        int? ActiveBatchNumber,
        DateTime CreatedAt,
        DateTime? ExpiresAt
    );
}
