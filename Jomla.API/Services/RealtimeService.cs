using Jomla.API.Hubs;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Jomla.API
{
    public class RealtimeService(IHubContext<JomlaHub, IJomlaClient> _hubContext) : IRealtimeService
    {
        public async Task SendNotificationAsync(Guid userId, NotificationDto notification)
        {
            await _hubContext.Clients
                .User(userId.ToString())
                .NotificationReceived(notification);
        }
    }
}
