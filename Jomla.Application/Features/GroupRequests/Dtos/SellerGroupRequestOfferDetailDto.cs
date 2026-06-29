using Jomla.Domain;
using System;
using System.Collections.Generic;

namespace Jomla.Application.Features.GroupRequests.Dtos;

public sealed record SupplierGroupRequestOfferDetailDto(
    Guid Id,
    Guid GroupRequestId,
    Guid SupplierId,
    string SupplierName,
    decimal UnitPrice,
    decimal? MinUnitPrice,
    decimal CurrentUnitPrice,
    int QuantityAvailable,
    int? MinFallbackQuantity,
    string? VariantAttributes,
    int RoundNumber,
    GroupRequestOfferStatus Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    int AcceptedQuantity,
    List<SupplierBuyerResponseDto> Responses
);

public sealed record SupplierBuyerResponseDto(
    Guid BuyerId,
    string BuyerName,
    string BuyerEmail,
    BuyerOfferResponseType Response,
    DateTime RespondedAt,
    int Quantity
);
