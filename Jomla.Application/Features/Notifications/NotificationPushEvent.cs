using Jomla.Application.Features.Notifications.DTOs;
using MediatR;

namespace Jomla.Application.Features.Notifications
{
    public sealed record NotificationPushEvent(Guid UserId, NotificationDto Notification) : INotification;
}
