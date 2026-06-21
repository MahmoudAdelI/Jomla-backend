using Jomla.Application.Features.Batches.DTOs;
using MediatR;

namespace Jomla.Application.Features.Batches.Queries
{
    public sealed record GetBatchDetailQuery(Guid BatchId) : IRequest<BatchDetailDto>;
}
