using MediatR;
using System;

namespace Jomla.Application.Features.GroupRequests.Commands.FailGroupRequestOffer
{
    public record FailGroupRequestOfferCommand(Guid OfferId) : IRequest;
}
