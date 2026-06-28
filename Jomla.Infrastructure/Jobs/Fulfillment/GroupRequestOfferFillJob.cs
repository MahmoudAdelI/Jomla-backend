using Jomla.Application.Features.GroupRequests.Commands.CompleteGroupRequestOffer;
using Jomla.Application.Jobs.Fulfillment;
using MediatR;

namespace Jomla.Infrastructure.Jobs.Fulfillment
{
    public class GroupRequestOfferFillJob(ISender sender) : IGroupRequestOfferFillJob
    {
        public async Task ExecuteAsync(Guid offerId)
        {
            await sender.Send(new CompleteGroupRequestOfferCommand(offerId));
        }
    }
}
