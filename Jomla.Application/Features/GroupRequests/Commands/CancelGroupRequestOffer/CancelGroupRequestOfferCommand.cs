using MediatR;
using System;

namespace Jomla.Application.Features.GroupRequests.Commands.CancelGroupRequestOffer
{
    public record CancelGroupRequestOfferCommand(Guid OfferId) : IRequest;
}
