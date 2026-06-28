using Jomla.Application.Features.GroupRequests.Commands.CompleteGroupRequestOffer;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Application.Jobs.Sync;
using MediatR;

namespace Jomla.Infrastructure.Jobs.Fulfillment
{
    public class GroupRequestOfferFillJob(ISender sender, IBackgroundJobDispatcher jobDispatcher) : IGroupRequestOfferFillJob
    {
        public async Task ExecuteAsync(Guid offerId)
        {
            await sender.Send(new CompleteGroupRequestOfferCommand(offerId));

            // Index after the offer is Accepted — guarantees the indexer sees the final state.
            jobDispatcher.Enqueue<INegotiationRoundIndexJob>(j => j.ExcuteAsync(offerId));
        }
    }
}
