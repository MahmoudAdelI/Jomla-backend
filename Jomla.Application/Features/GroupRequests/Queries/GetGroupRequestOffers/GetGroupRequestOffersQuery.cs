using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Domain;
using MediatR;

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOffers;

public sealed record GetGroupRequestOffersQuery: IRequest<List<BuyerGroupRequestOfferDto>>
{
    public Guid GroupRequestId { get; set; }

    public GroupRequestOfferStatus? Status { get; init; }
}
