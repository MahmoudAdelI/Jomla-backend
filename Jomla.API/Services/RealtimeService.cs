using Jomla.API.Hubs;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.DTOs;
using Jomla.Application.Features.Notifications.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Jomla.API.Services
{
    public class RealtimeService(IHubContext<JomlaHub, IJomlaClient> hubContext) : IRealtimeService
    {
        private readonly IHubContext<JomlaHub, IJomlaClient> _hubContext = hubContext;

        public async Task SendNotificationAsync(Guid userId, NotificationDto notification)
        {
            await _hubContext.Clients
                .User(userId.ToString())
                .NotificationReceived(notification);
        }
        public async Task SendBatchUpdatedAsync(Guid offerId, BatchUpdatedDto update)
        {
            await _hubContext.Clients
                .Group(HubGroups.OfferGroup(offerId))
                .BatchUpdated(update);
        }
    }
}
