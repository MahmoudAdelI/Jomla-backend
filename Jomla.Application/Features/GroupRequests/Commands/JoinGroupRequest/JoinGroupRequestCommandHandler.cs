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
using Jomla.Application.Features.GroupRequests.Commands.ReactivateGroupRequest;

namespace Jomla.Application.Features.GroupRequests.Commands.JoinGroupRequest
{
    public sealed class JoinGroupRequestCommandHandler : IRequestHandler<JoinGroupRequestCommand, JoinGroupRequestResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IBackgroundJobDispatcher _jobDispatcher;
        private readonly IMediator _mediator;
        private readonly IRealtimeService _realtimeService;

        public JoinGroupRequestCommandHandler(
            IAppDbContext context,
            IBackgroundJobDispatcher jobDispatcher,
            IMediator mediator,
            IRealtimeService realtimeService)
        {
            _context = context;
            _jobDispatcher = jobDispatcher;
            _mediator = mediator;
            _realtimeService = realtimeService;
        }

        public async Task<JoinGroupRequestResponse> Handle(JoinGroupRequestCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var groupRequest = await _context.GetGroupRequestWithLockAsync(request.GroupRequestId, cancellationToken);

            if (groupRequest == null)
                return new JoinGroupRequestResponse(false, "Group request not found.");

            // Validate
            if (groupRequest.Status == GroupRequestStatus.Closed)
                return new JoinGroupRequestResponse(false, "Group request is closed.");

            if (groupRequest.ModerationStatus == ModerationStatus.Flagged)
                return new JoinGroupRequestResponse(false, "Group request is flagged.");

            if (groupRequest.ModerationStatus != ModerationStatus.Approved)
                return new JoinGroupRequestResponse(false, "Group request is not approved yet.");

            var existingParticipant = await _context.GroupRequestParticipants
                .FirstOrDefaultAsync(p => p.GroupRequestId == request.GroupRequestId
                                       && p.BuyerId == request.BuyerId, cancellationToken);

            if (existingParticipant != null)
            {
                if (existingParticipant.Status == GroupRequestParticipantStatus.Active)
                {
                    return new JoinGroupRequestResponse(false, "You already joined this group request.");
                }

                // User is rejoining: update existing record
                existingParticipant.Status = GroupRequestParticipantStatus.Active;
                existingParticipant.Quantity = request.Quantity;
                existingParticipant.JoinedAt = DateTime.UtcNow; // Explicitly set timestamp on update (defaults only run on INSERT)
            }
            else
            {
                // Create new participant record (JoinedAt is database-generated via default constraint)
                var participant = new GroupRequestParticipant
                {
                    GroupRequestId = request.GroupRequestId,
                    BuyerId = request.BuyerId,
                    Quantity = request.Quantity,
                    Status = GroupRequestParticipantStatus.Active
                };

                _context.GroupRequestParticipants.Add(participant);
            }

            var wasInactive = groupRequest.Status == GroupRequestStatus.Inactive;

            groupRequest.CurrentQuantity += request.Quantity;

            if (wasInactive)
            {
                // Delegate reactivation to ReactivateGroupRequestCommand which enqueues its own matching job
                await _mediator.Send(new ReactivateGroupRequestCommand(request.GroupRequestId), cancellationToken);
            }
            else
            {
                await _context.SaveChangesAsync(cancellationToken);

                // Fire the SupplierMatchingJob
                _jobDispatcher.Enqueue<ISupplierMatchingJob>(j =>
                    j.ExecuteAsync(request.GroupRequestId, groupRequest.CategoryId, groupRequest.CurrentQuantity));
            }

            await transaction.CommitAsync(cancellationToken);

            try
            {
                var detail = await _mediator.Send(new Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestDetailQuery(request.GroupRequestId), cancellationToken);
                if (detail != null)
                {
                    await _realtimeService.SendGroupRequestUpdatedAsync(request.GroupRequestId, detail);
                }
            }
            catch
            {
                // Prevent SignalR exceptions from blocking user action
            }

            return new JoinGroupRequestResponse(true, null);
        }
    }
}
