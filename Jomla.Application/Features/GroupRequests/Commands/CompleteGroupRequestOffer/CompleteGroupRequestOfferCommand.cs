using MediatR;
using System;

namespace Jomla.Application.Features.GroupRequests.Commands.CompleteGroupRequestOffer
{
    public record CompleteGroupRequestOfferCommand(Guid OfferId) : IRequest;
}
