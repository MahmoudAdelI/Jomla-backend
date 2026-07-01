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
      int AcceptedQuantity
  ) : IRequest<AcceptGroupRequestOfferResponse>;


    public class AcceptGroupRequestOfferResponse
    {
        public Guid OfferId { get; set; }
        public Guid GroupRequestId { get; set; }
        public string PaymentIntentId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
        public int AcceptedQuantity { get; set; }
        public decimal TotalAmount { get; set; }
    }

}
