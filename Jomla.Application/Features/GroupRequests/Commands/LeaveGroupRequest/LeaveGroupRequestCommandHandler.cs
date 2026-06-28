using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Closing;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.LeaveGroupRequest
{
    public sealed class LeaveGroupRequestCommandHandler : IRequestHandler<LeaveGroupRequestCommand, LeaveGroupRequestResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IBackgroundJobDispatcher _jobDispatcher;

        public LeaveGroupRequestCommandHandler(
            IAppDbContext context,
            IBackgroundJobDispatcher jobDispatcher)
        {
            _context = context;
            _jobDispatcher = jobDispatcher;
        }

        public async Task<LeaveGroupRequestResponse> Handle(LeaveGroupRequestCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var groupRequest = await _context.GetGroupRequestWithLockAsync(request.GroupRequestId, cancellationToken);

            if (groupRequest == null)
                return new LeaveGroupRequestResponse(false, "Group request not found.");

            if (groupRequest.Status == GroupRequestStatus.Closed)
                return new LeaveGroupRequestResponse(false, "Group request is closed.");

            var participant = await _context.GroupRequestParticipants
                .FirstOrDefaultAsync(p => p.GroupRequestId == request.GroupRequestId
                                       && p.BuyerId == request.BuyerId
                                       && p.Status == GroupRequestParticipantStatus.Active, cancellationToken);

            if (participant == null)
                return new LeaveGroupRequestResponse(false, "You are not a member of this group request.");

            participant.Status = GroupRequestParticipantStatus.Left;

            groupRequest.CurrentQuantity -= participant.Quantity;

            if (groupRequest.CurrentQuantity <= 0)
            {
                groupRequest.CurrentQuantity = 0;
                groupRequest.Status = GroupRequestStatus.Inactive;
                groupRequest.InactiveSince = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _jobDispatcher.Schedule<IGroupRequestAutoCloseJob>(
                    j => j.ExecuteAsync(request.GroupRequestId),
                    DateTimeOffset.UtcNow.AddHours(24));
            }
            else
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return new LeaveGroupRequestResponse(true, null);
        }
    }
}
