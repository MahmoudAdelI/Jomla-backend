using Jomla.Application.Features.Batches.DTOs;
using Jomla.Application.Features.Notifications.DTOs;

namespace Jomla.API.Hubs
{
    public interface IJomlaClient
    {
        Task NotificationReceived(NotificationDto notification);
        Task BatchUpdated(BatchUpdatedDto update);
    }
}
