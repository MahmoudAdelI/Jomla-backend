using MediatR;

namespace Jomla.Application.Features.Batches.Commands
{
    public class LeaveBatchCommand : IRequest<LeaveBatchResponse>
    {
        public Guid BatchId { get; set; }
        public Guid BuyerId { get; set; }
    }

    public class LeaveBatchResponse
    {
        public bool Success { get; set; }
        public Guid? BatchId { get; set; }
        public int? RemainingQuantity { get; set; }
        public string Error { get; set; }
    }
}