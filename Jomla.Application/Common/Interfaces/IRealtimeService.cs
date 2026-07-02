using Jomla.Application.Features.Batches.DTOs;
using Jomla.Application.Features.Notifications.DTOs;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Application.Features.Offers.DTOs;

namespace Jomla.Application.Common.Interfaces
{
    public interface IRealtimeService
    {
        Task SendNotificationAsync(Guid userId, NotificationDto notification);
        Task SendBatchUpdatedAsync(Guid offerId, BatchUpdatedDto update);
        Task SendGroupRequestUpdatedAsync(Guid groupRequestId, GroupRequestDetailDto update);
        Task SendOfferStatusChangedAsync(Guid supplierId, OfferDto offer);
        Task SendFlaggedItemCreatedAsync(string entityType, Guid entityId);
        Task SendFlaggedItemResolvedAsync(Guid entityId);
        Task SendUserBatchStatusChangedAsync(Guid buyerId, Guid batchId, string newStatus);
    }
}
