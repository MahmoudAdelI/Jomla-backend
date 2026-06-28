using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Queries
{
    public sealed class GetGroupRequestDetailQueryHandler : IRequestHandler<GetGroupRequestDetailQuery, GroupRequestDetailDto?>
    {
        private readonly IAppDbContext _context;

        public GetGroupRequestDetailQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<GroupRequestDetailDto?> Handle(GetGroupRequestDetailQuery request, CancellationToken cancellationToken)
        {
            var groupRequest = await _context.GroupRequests
                .Include(r => r.Category)
                .Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == request.GroupRequestId, cancellationToken);

            if (groupRequest == null)
                return null;

            var imageUrlsList = string.IsNullOrEmpty(groupRequest.ImageUrls)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(groupRequest.ImageUrls) ?? new List<string>();

            return new GroupRequestDetailDto(
                groupRequest.Id,
                groupRequest.Title,
                groupRequest.Description,
                imageUrlsList,
                groupRequest.CurrentQuantity,
                groupRequest.Status.ToString(),
                groupRequest.ModerationStatus.ToString(),
                groupRequest.ModerationReason,
                groupRequest.CreatedAt,
                groupRequest.InitiatorId,
                groupRequest.Category.Name,
                groupRequest.Participants.Count(p => p.Status == GroupRequestParticipantStatus.Active)
            );
        }
    }
}
