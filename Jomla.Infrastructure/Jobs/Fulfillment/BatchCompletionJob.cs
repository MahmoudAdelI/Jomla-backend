using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.Commands.CompleteBatch;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Infrastructure.Jobs.Fulfillment
{
    public class BatchCompletionJob : IBatchCompletionJob
    {
        private readonly IAppDbContext _context;
        private readonly IMediator _mediator;

        public BatchCompletionJob(IAppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task ExecuteAsync(Guid batchId)
        {
            var batch = await _context.SupplierBatches
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null || batch.Status != BatchStatus.Open)
                return;

            if (batch.CurrentQuantity < batch.TargetQuantity)
                return;

            await _mediator.Send(new CompleteBatchCommand(batchId));
        }
    }
}
