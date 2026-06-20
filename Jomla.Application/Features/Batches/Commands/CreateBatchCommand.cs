using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Commands
{
    public class CreateBatchCommand:IRequest<CreateBatchResponse>
    {
        public Guid OfferId { get; set; }
        public int BatchNumber { get; set; }
    }

    public class CreateBatchResponse
    {
        public bool Success { get; set; }
        public Guid? BatchId { get; set; }
        public int? TargetQuantity { get; set; }
        public int? BatchNumber { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}

