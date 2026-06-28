using MediatR;

namespace Jomla.Application.Features.GroupRequests.Commands.PlaceGroupRequestOffer;

public sealed record PlaceGroupRequestOfferCommand : IRequest<Guid>
{
    public Guid GroupRequestId { get; set; }

    public decimal UnitPrice { get; init; }

    public decimal? MinUnitPrice { get; init; }

    public int QuantityAvailable { get; init; }

    public int? MinFallbackQuantity { get; init; }

    public string? VariantAttributes { get; init; }

    public DateTime ExpiresAt { get; init; }
}