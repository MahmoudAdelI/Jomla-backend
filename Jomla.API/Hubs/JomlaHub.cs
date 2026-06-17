using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Jomla.API.Hubs
{
    [Authorize]
    public class JomlaHub : Hub<IJomlaClient>
    {
        public async Task JoinBatch(int batchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"batch:{batchId}");
        }

        public async Task LeaveBatch(int batchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"batch:{batchId}");
        }

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
