using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Application.Features.GroupRequests.Queries.GetGroupRequests;
using Jomla.Domain;
using MediatR;

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOffers;

public sealed record GetGroupRequestOffersQuery: IRequest<PagedResult<BuyerGroupRequestOfferDto>>
{
    public Guid GroupRequestId { get; set; }

    public GroupRequestOfferStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}
