using MediatR;
using System;

namespace Jomla.Application.Features.GroupRequests.Commands.RejectGroupRequestOffer
{
    public record RejectGroupRequestOfferCommand(Guid OfferId, Guid BuyerId) : IRequest<RejectGroupRequestOfferResponse>;

    public class RejectGroupRequestOfferResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
