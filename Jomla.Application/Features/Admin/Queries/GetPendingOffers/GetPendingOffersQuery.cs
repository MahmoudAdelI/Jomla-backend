using Jomla.Application.Common.BaseClass;
using Jomla.Application.Features.Admin.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Queries.GetPendingOffers
{
    public sealed record GetPendingOffersQuery : PaginationQuery, IRequest<PagedResponse<FlaggedOfferDto>>;
}
