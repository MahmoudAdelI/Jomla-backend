using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Jomla.Application.Features.GroupRequests.Commands.ModerateGroupRequest
{
    public class ModerateGroupRequestCommandHandler(
        IAppDbContext db,
        IModerationAgent moderation,
        IMediator mediator) : IRequestHandler<ModerateGroupRequestCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly IModerationAgent _moderation = moderation;
        private readonly IMediator _mediator = mediator;

        public async Task Handle(ModerateGroupRequestCommand request, CancellationToken cancellationToken)
        {
            var groupRequest = await _db.GroupRequests
                .FirstOrDefaultAsync(r => r.Id == request.GroupRequestId, cancellationToken);

            if (groupRequest is null) return;

            if (groupRequest.ModerationStatus != ModerationStatus.Pending)
                return;

            var imageUrls = string.IsNullOrWhiteSpace(groupRequest.ImageUrls)
                ? []
                : JsonSerializer.Deserialize<List<string>>(groupRequest.ImageUrls) ?? [];

            var moderationInput = new ModerationInput(groupRequest.Title, groupRequest.Description, imageUrls);
            var result = await _moderation.ModerateAsync(moderationInput, cancellationToken);

            groupRequest.ModerationStatus = result.IsApproved
                ? ModerationStatus.Approved
                : ModerationStatus.Flagged;

            groupRequest.ModerationReason = result.Reason;

            groupRequest.Status = result.IsApproved
                ? GroupRequestStatus.Active
                : GroupRequestStatus.Inactive;

            var notification = new Notification
            {
                UserId = groupRequest.InitiatorId,
                Type = result.IsApproved ? NotificationType.GroupRequestApproved : NotificationType.GroupRequestFlagged,
                Title = result.IsApproved ? "Your group request has been approved" : "Your group request has been flagged",
                Body = result.IsApproved
                    ? "Your group request is now live and visible to suppliers."
                    : $"Your group request was flagged: {result.Reason}",
                EntityId = groupRequest.Id,
                EntityType = nameof(GroupRequest),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), cancellationToken);
        }
    }
}
