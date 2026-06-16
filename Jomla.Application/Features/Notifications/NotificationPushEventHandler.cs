using Jomla.Application.Common.Interfaces;
using MediatR;

namespace Jomla.Application.Features.Notifications
{
    public class NotificationPushEventHandler(IRealtimeService _realtimeService) : INotificationHandler<NotificationPushEvent>
    {
        public async Task Handle(NotificationPushEvent e, CancellationToken cancellationToken)
        {
            await _realtimeService.SendNotificationAsync(e.UserId, e.Notification);
        }
    }
}
