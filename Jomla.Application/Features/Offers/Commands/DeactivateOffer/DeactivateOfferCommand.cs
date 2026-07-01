using MediatR;

namespace Jomla.Application.Features.Offers.Commands.DeactivateOffer;

public sealed record DeactivateOfferCommand(Guid OfferId) : IRequest<bool>;
