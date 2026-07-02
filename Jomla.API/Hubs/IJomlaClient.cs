using Jomla.Application.Features.Batches.DTOs;
using Jomla.Application.Features.Notifications.DTOs;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Application.Features.Offers.DTOs;

namespace Jomla.API.Hubs
{
    public interface IJomlaClient
    {
        Task NotificationReceived(NotificationDto notification);
        Task BatchUpdated(BatchUpdatedDto update);
        Task GroupRequestUpdated(GroupRequestDetailDto update);
        Task OfferStatusChanged(OfferDto offer);
        Task FlaggedItemCreated(string entityType, Guid entityId);
        Task FlaggedItemResolved(Guid entityId);
        Task UserBatchStatusChanged(Guid batchId, string newStatus);
    }
}
