using MediatR;

namespace Jomla.Application.Features.Batches.Commands.CreateBatch
{
    public sealed record CreateBatchCommand(Guid OfferId) : IRequest<Guid?>;
}

