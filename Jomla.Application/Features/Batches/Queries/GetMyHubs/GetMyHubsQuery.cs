using System;
using System.Collections.Generic;
using MediatR;

namespace Jomla.Application.Features.Batches.Queries.GetMyHubs
{
    public record GetMyHubsQuery(Guid BuyerId) : IRequest<List<BuyerHubDto>>;
}
