using Jomla.Application.Features.Notifications.DTOs;

namespace Jomla.Application.Common.Interfaces
{
    public interface IRealtimeService
    {
        Task SendNotificationAsync(Guid userId, NotificationDto notification);
    }
}
