using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.AcceptGroupRequestOffer
{
    public record AcceptGroupRequestOfferCommand(
      Guid OfferId,
      Guid BuyerId,
      string BuyerEmail,
      bool ConfirmPartialQuantity = false  // Buyer agreed to accept the reduced quantity
  ) : IRequest<AcceptGroupRequestOfferResponse>;


    public class AcceptGroupRequestOfferResponse
    {
        public bool Success { get; set; }
        public bool RequiresConfirmation { get; set; }  // True when buyer needs to confirm a reduced quantity
        public Guid? OfferId { get; set; }
        public Guid? GroupRequestId { get; set; }
        public string? PaymentIntentId { get; set; }
        public int? AcceptedQuantity { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public int? AvailableSlots { get; set; }  // Remaining slots when quantity exceeds capacity
    }
}
