using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Jobs.Agents;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Infrastructure.Persistance;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Jomla.Infrastructure.Jobs.Agents
{
    public class ModerateGroupRequestJob(
        IDbContextFactory<AppDbContext> contextFactory,
        IModerationService moderation,
        IMediator mediator
        ) : IModerateGroupRequestJob
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;
        private readonly IModerationService _moderation = moderation;
        private readonly IMediator _mediator = mediator;

        public async Task ExecuteAsync(Guid groupRequestId, CancellationToken ct)
        {
            await using var db = await _contextFactory.CreateDbContextAsync(ct);

            var groupRequest = await db.GroupRequests
                .FirstOrDefaultAsync(r => r.Id == groupRequestId, ct);

            if (groupRequest is null) return;

            var imageUrls = string.IsNullOrWhiteSpace(groupRequest.ImageUrls)
                ? []
                : JsonSerializer.Deserialize<List<string>>(groupRequest.ImageUrls) ?? [];

            var moderationInput = new ModerationInput(groupRequest.Title, groupRequest.Description, imageUrls);
            var result = await _moderation.ModerateAsync(moderationInput, ct);

            groupRequest.ModerationStatus = result.IsApproved
                ? ModerationStatus.Approved
                : ModerationStatus.Flagged;

            groupRequest.ModerationReason = result.Reason;


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
            };

            db.Notifications.Add(notification);
            await db.SaveChangesAsync(ct);

            await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), ct);

        }
    }
}
