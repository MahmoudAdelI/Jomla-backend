using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Queries.GetSupplierMatchedGroupRequests
{
    public sealed class GetSupplierMatchedGroupRequestsQueryHandler
     : IRequestHandler<GetSupplierMatchedGroupRequestsQuery, PagedResult<SupplierMatchedGroupRequestDto>>
    {
        private readonly IAppDbContext _context;

        public GetSupplierMatchedGroupRequestsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<SupplierMatchedGroupRequestDto>> Handle(
            GetSupplierMatchedGroupRequestsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.GroupRequestAlerts
                .AsNoTracking()
                .Where(a => a.SupplierId == request.SupplierId
                         && a.Status != GroupRequestAlertStatus.Ignored
                         && a.GroupRequest.Status != GroupRequestStatus.Closed)
                .Include(a => a.GroupRequest)
                    .ThenInclude(r => r.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(a => a.GroupRequest.Title.ToLower().Contains(search) || 
                                         (a.GroupRequest.Description != null && a.GroupRequest.Description.ToLower().Contains(search)));
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(a => a.GroupRequest.CategoryId == request.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Status) && !request.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse<GroupRequestAlertStatus>(request.Status, true, out var statusEnum))
                {
                    query = query.Where(a => a.Status == statusEnum);
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.GroupRequest.CurrentQuantity)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => new SupplierMatchedGroupRequestDto(
                    a.GroupRequest.Id,
                    a.GroupRequest.Title,
                    a.GroupRequest.Description,
                    a.GroupRequest.CurrentQuantity,
                    a.GroupRequest.Status.ToString(),
                    a.GroupRequest.Category.Name,
                    a.GroupRequest.CreatedAt,
                    a.Status
                ))
                .ToListAsync(cancellationToken);

            return new PagedResult<SupplierMatchedGroupRequestDto>(items, totalCount, request.Page, request.PageSize);
        }
    }
}
