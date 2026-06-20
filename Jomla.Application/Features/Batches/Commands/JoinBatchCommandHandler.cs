using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Batches.Commands
{
    public class JoinBatchCommandHandler : IRequestHandler<JoinBatchCommand, JoinBatchResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IBackgroundJobDispatcher _jobDispatcher;

        public JoinBatchCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            IBackgroundJobDispatcher jobDispatcher)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _jobDispatcher = jobDispatcher; 
        }

        public async Task<JoinBatchResponse> Handle(JoinBatchCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Step 1: Fetch batch with offer
                var batch = await _context.SupplierBatches
                    .Include(b => b.Offer)
                    .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

                if (batch == null)
                {
                    return new JoinBatchResponse
                    {
                        Success = false,
                        Error = "Batch not found.",
                        ErrorCode = "BATCH_NOT_FOUND"
                    };
                }

                // Step 2: Validate batch is Open
                if (batch.Status != BatchStatus.Open)
                {
                    return new JoinBatchResponse
                    {
                        Success = false,
                        Error = $"Batch is {batch.Status}. Cannot join.",
                        ErrorCode = "BATCH_NOT_OPEN",
                        StatusCode = 409
                    };
                }

                // Step 3: Validate space available
                int spaceRemaining = batch.TargetQuantity - batch.CurrentQuantity;
                if (request.Quantity > spaceRemaining)
                {
                    return new JoinBatchResponse
                    {
                        Success = false,
                        Error = $"Only {spaceRemaining} slots available.",
                        ErrorCode = "INSUFFICIENT_SLOTS",
                        SlotsAvailable = spaceRemaining
                    };
                }

                // Step 4: Calculate amount
                decimal totalAmount = request.Quantity * batch.Offer.UnitPrice;

                // Step 5: Create Stripe PaymentIntent (HOLD)
                var paymentResult = await _stripePaymentService.CreatePaymentHoldAsync(
                    request.BuyerId.ToString(),
                    request.BuyerEmail,
                    totalAmount,
                    request.BatchId);

                if (!paymentResult.Success)
                {
                    return new JoinBatchResponse
                    {
                        Success = false,
                        Error = $"Payment hold failed: {paymentResult.Error}",
                        ErrorCode = "PAYMENT_HOLD_FAILED"
                    };
                }

                // Step 6: Create BatchParticipant
                var participant = new BatchParticipant
                {
                    BatchId = request.BatchId,
                    BuyerId = request.BuyerId,
                    Quantity = request.Quantity,
                    StripePaymentIntentId = paymentResult.PaymentIntentId,
                    Status = BatchParticipantStatus.Active,
                    JoinedAt = DateTime.UtcNow
                };

                _context.BatchParticipants.Add(participant);

                // Step 7: Increment batch quantity
                batch.CurrentQuantity += request.Quantity;

                // Step 8: Save
                await _context.SaveChangesAsync(cancellationToken);

                // Step 9: Check if batch complete
                //bool batchComplete = batch.CurrentQuantity >= batch.TargetQuantity;

                // Step 9: Fire background job
                _jobDispatcher.Enqueue<IBatchCompletionJob>(j => j.ExecuteAsync(request.BatchId));

                // Step 10: Return response
                return new JoinBatchResponse
                {
                    Success = true,
                    BatchId = request.BatchId,
                    ParticipantQuantity = request.Quantity,
                    TotalAmount = totalAmount,
                    PaymentIntentId = paymentResult.PaymentIntentId,
                    BatchCurrentQuantity = batch.CurrentQuantity,
                    BatchTargetQuantity = batch.TargetQuantity,
                    //BatchComplete = batchComplete
                };
            }
            catch (Exception ex)
            {
                return new JoinBatchResponse
                {
                    Success = false,
                    Error = $"Join batch failed: {ex.Message}",
                    ErrorCode = "INTERNAL_ERROR"
                };
            }
        }
    }
}