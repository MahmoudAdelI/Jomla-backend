using Jomla.Application.Common.Interfaces;
using MediatR;

namespace Jomla.Application.Features.Notifications
{
    public class NotificationCreatedEventHandler(IRealtimeService _realtimeService) : INotificationHandler<NotificationCreatedEvent>
    {
        public async Task Handle(NotificationCreatedEvent e, CancellationToken cancellationToken)
        {
            await _realtimeService.SendNotificationAsync(e.UserId, e.Notification);
        }
    }
}
