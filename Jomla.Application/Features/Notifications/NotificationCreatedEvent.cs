using MediatR;

namespace Jomla.Application.Features.Notifications
{
    public sealed record NotificationCreatedEvent(Guid UserId, Guid NotificationId) : INotification;
}
