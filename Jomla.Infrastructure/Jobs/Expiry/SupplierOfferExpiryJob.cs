using Jomla.Application.Features.Offers.Commands.ExpireSupplierOffer;
using Jomla.Application.Jobs.Expiry;
using MediatR;

namespace Jomla.Infrastructure.Jobs.Expiry
{
    public class SupplierOfferExpiryJob(ISender sender) : ISupplierOfferExpiryJob
    {
        private readonly ISender _sender = sender;

        public async Task ExecuteAsync(Guid offerId, CancellationToken ct)
        {
            await _sender.Send(new ExpireSupplierOfferCommand(offerId), ct);
        }
    }
}
