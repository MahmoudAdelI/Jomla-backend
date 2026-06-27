using MediatR;

namespace Jomla.Application.Features.Offers.Commands.ModerateSupplierOffer
{
    public sealed record ModerateSupplierOfferCommand(Guid OfferId) : IRequest;
}
