using Jomla.Application.Features.Offers.Commands.ModerateSupplierOffer;
using Jomla.Application.Jobs.Agents;
using MediatR;

namespace Jomla.Infrastructure.Jobs.Agents
{
    public class ModerateSupplierOfferJob(ISender sender) : IModerateSupplierOfferJob
    {
        private readonly ISender _sender = sender;

        public async Task ExecuteAsync(Guid offerId, CancellationToken ct)
        {
            await _sender.Send(new ModerateSupplierOfferCommand(offerId), ct);
        }
    }
}
