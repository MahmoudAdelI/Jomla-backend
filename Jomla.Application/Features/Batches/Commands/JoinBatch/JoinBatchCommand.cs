using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Jomla.Application.Features.Batches.Commands.JoinBatch
{

        public class JoinBatchCommand : IRequest<JoinBatchResponse>
        {
            public Guid BatchId { get; set; }
            public Guid BuyerId { get; set; }
            public string BuyerEmail { get; set; }
            public int Quantity { get; set; }
            //public bool ConfirmPartialQuantity { get; set; } = false 
        }

        public class JoinBatchResponse
        {
            public bool Success { get; set; }
            public Guid? BatchId { get; set; }
            public int? ParticipantQuantity { get; set; }
            public decimal? TotalAmount { get; set; }
            public string PaymentIntentId { get; set; }
            public string ClientSecret { get; set; }
            public int? BatchCurrentQuantity { get; set; }
            public int? BatchTargetQuantity { get; set; }
            //public bool BatchComplete { get; set; }
            public string Error { get; set; }
            public string ErrorCode { get; set; }
            public int? SlotsAvailable { get; set; }
            public int? StatusCode { get; set; }
        }
    
}
