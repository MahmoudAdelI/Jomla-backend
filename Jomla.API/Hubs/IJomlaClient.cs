using Jomla.Application.Features.Notifications.DTOs;

namespace Jomla.API.Hubs
{
    public interface IJomlaClient
    {
        Task NotificationReceived(NotificationDto notification);
    }
}
