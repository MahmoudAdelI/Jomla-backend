using System;

namespace Jomla.Application.Features.GroupRequests.Dtos;

public sealed record GroupRequestOfferDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    decimal UnitPrice,
    decimal? MinUnitPrice,
    decimal CurrentUnitPrice,
    int QuantityAvailable,
    int? MinFallbackQuantity,
    int AcceptedQuantity,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    int RoundNumber,
    List<Guid> AcceptedBuyerIds
);
