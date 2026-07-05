using System;

namespace Jomla.Application.Features.GroupRequests.Dtos;

public sealed record SupplierGroupRequestOfferDto(
    Guid Id,
    Guid GroupRequestId,
    string GroupRequestTitle,
    decimal UnitPrice,
    int QuantityAvailable,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string? VariantAttributes
);
