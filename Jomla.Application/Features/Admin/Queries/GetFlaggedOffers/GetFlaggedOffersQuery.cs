using Jomla.Application.Features.Admin.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Queries.GetFlaggedOffers
{
    public sealed record GetFlaggedOffersQuery(int Page = 1, int PageSize = 10)
    : IRequest<PagedResult<FlaggedOfferDto>>;
}
