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
        DateTime? ExpiresAt
    );
}
