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

namespace Jomla.Application.Features.Admin.Commands.RejectGroupRequest
{
    public sealed class RejectGroupRequestCommandHandler : IRequestHandler<RejectGroupRequestCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IMediator _mediator;

        public RejectGroupRequestCommandHandler(IAppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task Handle(RejectGroupRequestCommand request, CancellationToken cancellationToken)
        {
            var groupRequest = await _context.GroupRequests
                .FirstOrDefaultAsync(r => r.Id == request.GroupRequestId, cancellationToken);

            if (groupRequest == null || groupRequest.ModerationStatus != ModerationStatus.Flagged)
                return;

            groupRequest.ModerationStatus = ModerationStatus.Flagged;
            groupRequest.ModerationReason = request.Reason;

            var notification = new Notification
            {
                UserId = groupRequest.InitiatorId,
                Type = NotificationType.GroupRequestFlagged,
                Title = "Your group request has been rejected",
                Body = $"Your group request was rejected: {request.Reason}",
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
