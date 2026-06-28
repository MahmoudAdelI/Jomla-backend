using Jomla.Domain;

namespace Jomla.Application.Features.GroupRequests.Dtos;

public sealed record BuyerOfferResponseDto(
    Guid BuyerId,
    string BuyerName,
    BuyerOfferResponseType Response,
    DateTime RespondedAt);