using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Commands.UpdateBatch
{
    public record UpdateBatchParticipantQuantityCommand(Guid BatchId, Guid BuyerId, string BuyerEmail, int NewQuantity)
         : IRequest<UpdateBatchParticipantQuantityResponse>;

    public class UpdateBatchParticipantQuantityResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public int UpdatedQuantity { get; set; }
        public decimal NewTotalAmount { get; set; }
        public string NewPaymentIntentId { get; set; }
        public string ClientSecret { get; set; }
        public int BatchCurrentQuantity { get; set; }
    }

}
