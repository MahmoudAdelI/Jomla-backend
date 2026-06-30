using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Commands.ApproveGroupRequest
{
    public sealed class ApproveGroupRequestCommandHandler : IRequestHandler<ApproveGroupRequestCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IMediator _mediator;

        public ApproveGroupRequestCommandHandler(IAppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task Handle(ApproveGroupRequestCommand request, CancellationToken cancellationToken)
        {
            var groupRequest = await _context.GroupRequests
                .FirstOrDefaultAsync(r => r.Id == request.GroupRequestId, cancellationToken);

            if (groupRequest == null || groupRequest.ModerationStatus != ModerationStatus.Flagged)
                return;

            groupRequest.ModerationStatus = ModerationStatus.Approved;
            groupRequest.ModerationReason = null;
            groupRequest.Status = GroupRequestStatus.Active;

            var notification = new Notification
            {
                UserId = groupRequest.InitiatorId,
                Type = NotificationType.GroupRequestApproved,
                Title = "Your group request has been approved",
                Body = "Your group request has been reviewed and approved by our team.",
                EntityId = groupRequest.Id,
                EntityType = nameof(GroupRequest),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), cancellationToken);
        }
    }
}
