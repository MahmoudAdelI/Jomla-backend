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

namespace Jomla.Application.Features.Admin.Queries.GetFlaggedGroupRequests
{
    public sealed class GetFlaggedGroupRequestsQueryHandler : IRequestHandler<GetFlaggedGroupRequestsQuery, PagedResult<FlaggedGroupRequestDto>>
    {
        private readonly IAppDbContext _context;

        public GetFlaggedGroupRequestsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<FlaggedGroupRequestDto>> Handle(GetFlaggedGroupRequestsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.GroupRequests
                .Where(r => r.ModerationStatus == ModerationStatus.Flagged);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new FlaggedGroupRequestDto(
                    r.Id,
                    r.Title,
                    r.ModerationReason ?? "",
                    r.CreatedAt,
                    r.InitiatorId
                ))
                .ToListAsync(cancellationToken);

            return new PagedResult<FlaggedGroupRequestDto>(items, totalCount, request.Page, request.PageSize);
        }
    }
}
