using Jomla.Domain;
using Jomla.Domain.Entities;

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
    int AcceptedQuantity,
    GroupRequestOfferStatus Status,
    int RoundNumber,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string? VariantAttributes,
    List<BuyerOfferResponseDto> Responses);