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

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequests
{
    public sealed class GetGroupRequestsQueryHandler : IRequestHandler<GetGroupRequestsQuery, PagedResult<GroupRequestListItemDto>>
    {
        private readonly IAppDbContext _context;

        public GetGroupRequestsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<GroupRequestListItemDto>> Handle(GetGroupRequestsQuery request, CancellationToken cancellationToken)
        {
            var targetStatus = GroupRequestStatus.Active;
            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<GroupRequestStatus>(request.Status, out var parsedStatus))
            {
                targetStatus = parsedStatus;
            }

            var query = _context.GroupRequests
                .Include(r => r.Category)
                .Include(r => r.Participants)
                .Where(r => r.ModerationStatus == ModerationStatus.Approved
                         && r.Status == targetStatus)
                .AsQueryable();

            // Filtering
            if (request.CategoryId.HasValue)
                query = query.Where(r => r.CategoryId == request.CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(request.TitleSearch))
                query = query.Where(r => r.Title.Contains(request.TitleSearch));

            if (request.BuyerId.HasValue)
            {
                query = query.Where(r => r.InitiatorId == request.BuyerId.Value ||
                                         r.Participants.Any(p => p.BuyerId == request.BuyerId.Value && p.Status == GroupRequestParticipantStatus.Active));
            }

            // Total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Sorting
            if (request.SortBy == "most_buyers")
            {
                query = query.OrderByDescending(r => r.Participants.Count(p => p.Status == GroupRequestParticipantStatus.Active));
            }
            else if (request.SortBy == "newest")
            {
                query = query.OrderByDescending(r => r.CreatedAt);
            }
            else
            {
                query = query.OrderByDescending(r => r.CurrentQuantity);
            }

            // Pagination
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new GroupRequestListItemDto(
                    r.Id,
                    r.Title,
                    r.Description,
                    r.CurrentQuantity,
                    r.Status.ToString(),
                    r.Category.Name,
                    r.CreatedAt,
                    r.Participants.Count(p => p.Status == GroupRequestParticipantStatus.Active)
                ))
                .ToListAsync(cancellationToken);

            return new PagedResult<GroupRequestListItemDto>(items, totalCount, request.Page, request.PageSize);
        }
    }
}
