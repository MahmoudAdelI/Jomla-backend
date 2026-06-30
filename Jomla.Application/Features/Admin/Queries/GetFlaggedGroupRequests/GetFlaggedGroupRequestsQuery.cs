using Jomla.Application.Features.Admin.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Queries.GetFlaggedGroupRequests
{
    public sealed record GetFlaggedGroupRequestsQuery(int Page = 1, int PageSize = 10)
    : IRequest<PagedResult<FlaggedGroupRequestDto>>;

}
