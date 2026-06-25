using MediatR;

namespace Jomla.Application.Features.GroupRequests.Commands.NegotiateGroupRequestOffer
{
    public sealed record NegotiateGroupRequestOfferCommand(Guid OfferId, string categoryName) : IRequest;
}
