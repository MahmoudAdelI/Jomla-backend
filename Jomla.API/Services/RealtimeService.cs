using Jomla.API.Hubs;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.DTOs;
using Jomla.Application.Features.Notifications.DTOs;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Application.Features.Offers.DTOs;
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

        public async Task SendGroupRequestUpdatedAsync(Guid groupRequestId, GroupRequestDetailDto update)
        {
            await _hubContext.Clients
                .Group(HubGroups.GroupRequestGroup(groupRequestId))
                .GroupRequestUpdated(update);
        }

        public async Task SendOfferStatusChangedAsync(Guid supplierId, OfferDto offer)
        {
            await _hubContext.Clients
                .User(supplierId.ToString())
                .OfferStatusChanged(offer);
        }

        public async Task SendFlaggedItemCreatedAsync(string entityType, Guid entityId)
        {
            await _hubContext.Clients
                .Group(HubGroups.AdminGroup())
                .FlaggedItemCreated(entityType, entityId);
        }

        public async Task SendFlaggedItemResolvedAsync(Guid entityId)
        {
            await _hubContext.Clients
                .Group(HubGroups.AdminGroup())
                .FlaggedItemResolved(entityId);
        }

        public async Task SendUserBatchStatusChangedAsync(Guid buyerId, Guid batchId, string newStatus)
        {
            await _hubContext.Clients
                .User(buyerId.ToString())
                .UserBatchStatusChanged(batchId, newStatus);
        }
    }
}
