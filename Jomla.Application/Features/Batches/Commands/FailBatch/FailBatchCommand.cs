using MediatR;

namespace Jomla.Application.Features.Batches.Commands.FailBatch
{
    public sealed record FailBatchCommand(Guid BatchId) : IRequest;

}
