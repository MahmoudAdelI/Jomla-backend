using Jomla.Application.Common.BaseClass;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Admin.Dtos;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Queries.GetPendingGroupRequests
{
    public sealed class GetPendingGroupRequestsQueryHandler : IRequestHandler<GetPendingGroupRequestsQuery, PagedResponse<FlaggedGroupRequestDto>>
    {
        private readonly IAppDbContext _context;

        public GetPendingGroupRequestsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<FlaggedGroupRequestDto>> Handle(GetPendingGroupRequestsQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber ?? 1;
            var pageSize = request.PageSize ?? 10;

            var query = _context.GroupRequests
                .Where(r => r.ModerationStatus == ModerationStatus.Pending);
                         //&& r.CreatedAt < DateTime.UtcNow.AddMinutes(-30));

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower();
                query = query.Where(r => r.Title.ToLower().Contains(searchTerm) ||
                                         (r.ModerationReason != null && r.ModerationReason.ToLower().Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new FlaggedGroupRequestDto(
                    r.Id,
                    r.Title,
                    r.ModerationReason ?? "",
                    r.CreatedAt,
                    r.InitiatorId
                ))
                .ToListAsync(cancellationToken);

            return new PagedResponse<FlaggedGroupRequestDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
    }
}
