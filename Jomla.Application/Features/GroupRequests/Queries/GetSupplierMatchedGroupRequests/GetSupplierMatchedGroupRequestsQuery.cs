using Jomla.Application.Features.GroupRequests.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Queries.GetSupplierMatchedGroupRequests
{
    public sealed record GetSupplierMatchedGroupRequestsQuery(
        Guid SupplierId,
        int Page = 1,
        int PageSize = 10,
        string? Search = null,
        Guid? CategoryId = null,
        string? Status = null
    ) : IRequest<PagedResult<SupplierMatchedGroupRequestDto>>;
}
