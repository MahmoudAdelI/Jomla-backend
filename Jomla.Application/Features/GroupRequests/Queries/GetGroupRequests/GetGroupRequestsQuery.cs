using Jomla.Application.Features.GroupRequests.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequests
{
    public sealed record GetGroupRequestsQuery(
        Guid? CategoryId,
        string? TitleSearch,
        string? Status,
        int Page = 1,
        int PageSize = 10,
        string? SortBy = null,
        Guid? BuyerId = null
    ) : IRequest<PagedResult<GroupRequestListItemDto>>;
}


public sealed record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
