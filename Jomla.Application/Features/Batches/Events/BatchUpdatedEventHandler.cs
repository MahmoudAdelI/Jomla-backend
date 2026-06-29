using Jomla.Application.Common.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Events
{
    public class BatchUpdatedEventHandler(IRealtimeService realtimeService)
        : INotificationHandler<BatchUpdatedEvent>
    {
        private readonly IRealtimeService _realtimeService = realtimeService;

        public Task Handle(BatchUpdatedEvent e, CancellationToken ct)
            => _realtimeService.SendBatchUpdatedAsync(e.OfferId, e.Update);
    }
}
