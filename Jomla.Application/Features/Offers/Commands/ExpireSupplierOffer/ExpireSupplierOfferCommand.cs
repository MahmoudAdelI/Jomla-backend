using MediatR;

namespace Jomla.Application.Features.Offers.Commands.ExpireSupplierOffer
{
    public sealed record ExpireSupplierOfferCommand(Guid OfferId) : IRequest;
}
