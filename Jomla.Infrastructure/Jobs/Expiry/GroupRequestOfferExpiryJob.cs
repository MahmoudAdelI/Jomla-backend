using Jomla.Application.Features.GroupRequests.Commands.ExpireGroupRequestOffer;
using Jomla.Application.Jobs.Expiry;
using MediatR;

namespace Jomla.Infrastructure.Jobs.Expiry
{
    public class GroupRequestOfferExpiryJob(ISender sender) : IGroupRequestOfferExpiryJob
    {
        private readonly ISender _sender = sender;
        public async Task ExcuteAsync(Guid offerId)
        {
            await _sender.Send(new ExpireGroupRequestOfferCommand(offerId));
        }
    }
}
