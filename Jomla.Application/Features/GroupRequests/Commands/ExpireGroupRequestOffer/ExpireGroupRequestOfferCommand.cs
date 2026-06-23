using MediatR;

namespace Jomla.Application.Features.GroupRequests.Commands.ExpireGroupRequestOffer
{
    public sealed record ExpireGroupRequestOfferCommand(Guid OfferId) : IRequest;
}
