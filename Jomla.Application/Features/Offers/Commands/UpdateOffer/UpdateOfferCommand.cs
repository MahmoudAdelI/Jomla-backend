using MediatR;
using Microsoft.AspNetCore.Http;

namespace Jomla.Application.Features.Offers.Commands.UpdateOffer;

public sealed record UpdateOfferCommand(
    Guid Id,
    Guid CategoryId,
    string Title,
    string? Description,
    string? VariantAttributes,
    decimal UnitPrice,
    decimal DiscountPercentage,
    int TotalQuantityAvailable,
    int BatchTargetQuantity,
    int? MinFallbackQuantity,
    DateTime? ExpiresAt,
    List<IFormFile>? Images
) : IRequest<bool>;