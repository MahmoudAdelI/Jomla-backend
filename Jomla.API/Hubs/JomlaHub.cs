using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Jomla.API.Hubs
{
    [Authorize]
    public class JomlaHub : Hub<IJomlaClient>
    {
        public async Task JoinOfferGroup(string offerId)
        {
            if (Guid.TryParse(offerId, out var id))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.OfferGroup(id));
            }
            else
            {
                throw new HubException($"Invalid offer ID format: {offerId}");
            }
        }

        public async Task LeaveOfferGroup(string offerId)
        {
            if (Guid.TryParse(offerId, out var id))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.OfferGroup(id));
            }
            else
            {
                throw new HubException($"Invalid offer ID format: {offerId}");
            }
        }

        public async Task JoinGroupRequestGroup(string groupRequestId)
        {
            if (Guid.TryParse(groupRequestId, out var id))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.GroupRequestGroup(id));
            }
            else
            {
                throw new HubException($"Invalid group request ID format: {groupRequestId}");
            }
        }

        public async Task LeaveGroupRequestGroup(string groupRequestId)
        {
            if (Guid.TryParse(groupRequestId, out var id))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.GroupRequestGroup(id));
            }
            else
            {
                throw new HubException($"Invalid group request ID format: {groupRequestId}");
            }
        }

        public async Task JoinAdminGroup()
            => await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.AdminGroup());

        public async Task LeaveAdminGroup()
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.AdminGroup());

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
