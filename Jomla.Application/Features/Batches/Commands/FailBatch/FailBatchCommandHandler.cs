using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jomla.Application.Features.Batches.Commands.FailBatch
{
    public class FailBatchCommandHandler(
        IAppDbContext db,
        IStripePaymentService stripe,
        ILogger<FailBatchCommandHandler> logger
        ) : IRequestHandler<FailBatchCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly IStripePaymentService _stripe = stripe;
        private readonly ILogger<FailBatchCommandHandler> _logger = logger;

        public async Task Handle(FailBatchCommand request, CancellationToken cancellationToken)
        {
            // Load batch with active participants
            var batch = await _db.SupplierBatches
                .Include(b => b.Participants.Where(p => p.Status == BatchParticipantStatus.Active))
                .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

            if (batch is null || batch.Status != BatchStatus.Open) return;

            // Cancel Stripe hold for each participant individually so one failure doesn't abort the rest
            foreach (var participant in batch.Participants)
            {
                try
                {
                    await _stripe.CancelPaymentAsync(participant.StripePaymentIntentId, cancellationToken);
                }
                catch( Exception ex ) 
                {
                    _logger.LogError(ex,
                    "Failed to cancel Stripe hold for buyer {BuyerId} on batch {BatchId}. PaymentIntentId: {PaymentIntentId}",
                    participant.BuyerId, request.BatchId, participant.StripePaymentIntentId);
                }

                participant.Status = BatchParticipantStatus.Left;
            }

            batch.Status = BatchStatus.Failed;
            batch.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
