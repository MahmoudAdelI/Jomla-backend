using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Batches.Commands
{
    public class LeaveBatchCommandHandler : IRequestHandler<LeaveBatchCommand, LeaveBatchResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;

        public LeaveBatchCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
        }

        public async Task<LeaveBatchResponse> Handle(LeaveBatchCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Step 1: Fetch batch with participants
                var batch = await _context.SupplierBatches
                    .Include(b => b.Participants)
                    .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

                if (batch == null)
                {
                    return new LeaveBatchResponse
                    {
                        Success = false,
                        Error = "Batch not found."
                    };
                }

                // Step 2: Find participant
                var participant = batch.Participants
                    .FirstOrDefault(p => p.BuyerId == request.BuyerId && p.Status == BatchParticipantStatus.Active);

                if (participant == null)
                {
                    return new LeaveBatchResponse
                    {
                        Success = false,
                        Error = "You are not an active participant in this batch."
                    };
                }

                // Step 3: Cancel Stripe hold
                var cancelResult = await _stripePaymentService.CancelPaymentAsync(
                    participant.StripePaymentIntentId);

                if (!cancelResult.Success)
                {
                    return new LeaveBatchResponse
                    {
                        Success = false,
                        Error = $"Failed to cancel payment hold: {cancelResult.Error}"
                    };
                }

                // Step 4: Mark as Left
                participant.Status = BatchParticipantStatus.Left;

                // Step 5: Decrement batch quantity
                batch.CurrentQuantity -= participant.Quantity;

                // Step 6: Save
                await _context.SaveChangesAsync(cancellationToken);

                // Step 7: Return response
                return new LeaveBatchResponse
                {
                    Success = true,
                    BatchId = request.BatchId,
                    RemainingQuantity = batch.CurrentQuantity
                };
            }
            catch (Exception ex)
            {
                return new LeaveBatchResponse
                {
                    Success = false,
                    Error = $"Leave batch failed: {ex.Message}"
                };
            }
        }
    }
}