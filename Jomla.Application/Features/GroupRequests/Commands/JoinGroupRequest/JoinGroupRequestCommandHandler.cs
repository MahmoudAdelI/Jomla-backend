using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Application.Jobs.Matching;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.JoinGroupRequest
{
    public sealed class JoinGroupRequestCommandHandler : IRequestHandler<JoinGroupRequestCommand, JoinGroupRequestResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IBackgroundJobDispatcher _jobDispatcher;

        public JoinGroupRequestCommandHandler(
            IAppDbContext context,
            IBackgroundJobDispatcher jobDispatcher)
        {
            _context = context;
            _jobDispatcher = jobDispatcher;
        }

        public async Task<JoinGroupRequestResponse> Handle(JoinGroupRequestCommand request, CancellationToken cancellationToken)
        {
            
            var groupRequest = await _context.GroupRequests
                .FromSqlInterpolated($"SELECT * FROM group_requests WITH (UPDLOCK) WHERE Id = {request.GroupRequestId}")
                .FirstOrDefaultAsync(cancellationToken);

            if (groupRequest == null)
                return new JoinGroupRequestResponse(false, "Group request not found.");

            // Validate
            if (groupRequest.Status == GroupRequestStatus.Closed)
                return new JoinGroupRequestResponse(false, "Group request is closed.");

            if (groupRequest.ModerationStatus == ModerationStatus.Flagged)
                return new JoinGroupRequestResponse(false, "Group request is flagged.");

            if (groupRequest.ModerationStatus != ModerationStatus.Approved)
                return new JoinGroupRequestResponse(false, "Group request is not approved yet.");

            
            var alreadyJoined = await _context.GroupRequestParticipants
                .AnyAsync(p => p.GroupRequestId == request.GroupRequestId
                            && p.BuyerId == request.BuyerId
                            && p.Status == GroupRequestParticipantStatus.Active, cancellationToken);

            if (alreadyJoined)
                return new JoinGroupRequestResponse(false, "You already joined this group request.");

            
            if (groupRequest.Status == GroupRequestStatus.Inactive)
            {
                groupRequest.Status = GroupRequestStatus.Active;
                groupRequest.InactiveSince = null;
            }

           
            groupRequest.CurrentQuantity += request.Quantity;

            
            var participant = new GroupRequestParticipant
            {
                GroupRequestId = request.GroupRequestId,
                BuyerId = request.BuyerId,
                Quantity = request.Quantity,
                Status = GroupRequestParticipantStatus.Active,
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupRequestParticipants.Add(participant);
            await _context.SaveChangesAsync(cancellationToken);

            //  Fire الـSupplierMatchingJob
            _jobDispatcher.Enqueue<ISupplierMatchingJob>(j =>
                j.ExecuteAsync(request.GroupRequestId, groupRequest.CategoryId, groupRequest.CurrentQuantity));

            return new JoinGroupRequestResponse(true, null);
        }
    }
}
