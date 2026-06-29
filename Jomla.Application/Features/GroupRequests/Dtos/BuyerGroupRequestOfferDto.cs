using Jomla.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Dtos
{
    public sealed record BuyerGroupRequestOfferDto(
        Guid Id,

        Guid SupplierId,

        string SupplierName,

        decimal CurrentUnitPrice,

        int QuantityAvailable,

        int AcceptedQuantity,

        GroupRequestOfferStatus Status,

        int RoundNumber,

        DateTime ExpiresAt,

        string? VariantAttributes
    );
}
