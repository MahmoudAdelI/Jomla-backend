using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Jomla.API.Hubs
{
    [Authorize]
    public class JomlaHub : Hub<IJomlaClient>
    {
        public async Task JoinOfferGroup(Guid offerId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, $"offer:{offerId}");

        public async Task LeaveOfferGroup(Guid offerId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.OfferGroup(offerId));

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
