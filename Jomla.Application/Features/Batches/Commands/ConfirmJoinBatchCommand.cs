using MediatR;
using System;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.DTOs;
using Jomla.Application.Features.Batches.Events;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Commands
{
    public class ConfirmJoinBatchCommand : IRequest<ConfirmJoinBatchResponse>
    {
        public Guid BatchId { get; set; }
        public Guid BuyerId { get; set; }
        public int Quantity { get; set; }
        public string PaymentIntentId { get; set; }
    }

    public class ConfirmJoinBatchResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string ErrorCode { get; set; }
        public int? StatusCode { get; set; }
    }

    public class ConfirmJoinBatchCommandHandler : IRequestHandler<ConfirmJoinBatchCommand, ConfirmJoinBatchResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IBackgroundJobDispatcher _jobDispatcher;
        private readonly IMediator _mediator;

        public ConfirmJoinBatchCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            IBackgroundJobDispatcher jobDispatcher,
            IMediator mediator)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _jobDispatcher = jobDispatcher;
            _mediator = mediator;
        }

        public async Task<ConfirmJoinBatchResponse> Handle(ConfirmJoinBatchCommand request, CancellationToken cancellationToken)
        {
            // 1️⃣ Fetch batch with offer
            var batch = await _context.SupplierBatches
                .Include(b => b.Offer)
                .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

            if (batch == null)
            {
                return new ConfirmJoinBatchResponse
                {
                    Success = false,
                    Error = $"SupplierBatch with ID '{request.BatchId}' was not found.",
                    ErrorCode = "NOT_FOUND",
                    StatusCode = 404
                };
            }

            // 2️⃣ Validate batch is Open
            if (batch.Status != BatchStatus.Open)
            {
                return new ConfirmJoinBatchResponse
                {
                    Success = false,
                    Error = $"Batch is {batch.Status}. Cannot join.",
                    ErrorCode = "INVALID_BATCH_STATUS",
                    StatusCode = 409
                };
            }

            // 3️⃣ Check if already an ACTIVE participant
            var existingParticipant = await _context.BatchParticipants
                .FirstOrDefaultAsync(p => p.BatchId == request.BatchId
                                        && p.BuyerId == request.BuyerId, cancellationToken);

            if (existingParticipant != null && existingParticipant.Status == BatchParticipantStatus.Active)
            {
                return new ConfirmJoinBatchResponse
                {
                    Success = false,
                    Error = "You are already a participant in this batch.",
                    ErrorCode = "ALREADY_PARTICIPANT",
                    StatusCode = 400
                };
            }

            // 3.5️⃣ Re-validate capacity
            int spaceRemaining = batch.TargetQuantity - batch.CurrentQuantity;
            if (request.Quantity > spaceRemaining)
            {
                // Rollback Stripe payment hold to prevent charging buyer
                await _stripePaymentService.CancelPaymentAsync(request.PaymentIntentId, cancellationToken);
                return new ConfirmJoinBatchResponse
                {
                    Success = false,
                    Error = $"Only {spaceRemaining} slots available.",
                    ErrorCode = "INSUFFICIENT_SLOTS",
                    StatusCode = 409
                };
            }

            // 4️⃣ Verify Stripe PaymentIntent status
            var paymentResult = await _stripePaymentService.GetPaymentIntentAsync(request.PaymentIntentId, cancellationToken);
            if (!paymentResult.Success)
            {
                return new ConfirmJoinBatchResponse
                {
                    Success = false,
                    Error = $"Could not verify payment: {paymentResult.Error}",
                    ErrorCode = "PAYMENT_VERIFICATION_FAILED",
                    StatusCode = 400
                };
            }

            if (paymentResult.Status != "requires_capture" && paymentResult.Status != "succeeded")
            {
                return new ConfirmJoinBatchResponse
                {
                    Success = false,
                    Error = $"Stripe payment intent status is '{paymentResult.Status}', but 'requires_capture' or 'succeeded' is required.",
                    ErrorCode = "PAYMENT_NOT_AUTHORIZED",
                    StatusCode = 400
                };
            }

            // 4.5️⃣ Verify Payment amount matches request.Quantity
            decimal expectedAmount = request.Quantity * batch.Offer.UnitPrice * (1 - batch.Offer.DiscountPercentage / 100m);
            long expectedAmountInCents = (long)Math.Round(expectedAmount * 100, MidpointRounding.AwayFromZero);

            if (paymentResult.Amount != expectedAmountInCents)
            {
                // Rollback Stripe payment hold
                await _stripePaymentService.CancelPaymentAsync(request.PaymentIntentId, cancellationToken);
                return new ConfirmJoinBatchResponse
                {
                    Success = false,
                    Error = "Payment amount does not match the requested quantity.",
                    ErrorCode = "PAYMENT_AMOUNT_MISMATCH",
                    StatusCode = 400
                };
            }

            // 5️⃣ Reactivate existing record OR create new participant
            if (existingParticipant != null)
            {
                existingParticipant.Quantity = request.Quantity;
                existingParticipant.StripePaymentIntentId = request.PaymentIntentId;
                existingParticipant.Status = BatchParticipantStatus.Active;
                existingParticipant.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                _context.BatchParticipants.Add(new BatchParticipant
                {
                    BatchId = request.BatchId,
                    BuyerId = request.BuyerId,
                    Quantity = request.Quantity,
                    StripePaymentIntentId = request.PaymentIntentId,
                    Status = BatchParticipantStatus.Active,
                    JoinedAt = DateTime.UtcNow
                });
            }

            // 6️⃣ Update batch quantity
            batch.CurrentQuantity += request.Quantity;

            // 7️⃣ Save changes
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Rollback Stripe payment hold to prevent charging buyer when save fails
                await _stripePaymentService.CancelPaymentAsync(request.PaymentIntentId, cancellationToken);

                return new ConfirmJoinBatchResponse
                {
                    Success = false,
                    Error = "The batch was updated by another request. Please try again.",
                    ErrorCode = "CONCURRENCY_CONFLICT",
                    StatusCode = 409
                };
            }

            // 7.5️⃣ Publish batch update event
            try
            {
                var updateDto = BatchUpdatedDto.MapFrom(batch);
                await _mediator.Publish(new BatchUpdatedEvent(batch.OfferId, updateDto), cancellationToken);
            }
            catch (Exception)
            {
                // Warn but do not fail the core transaction if real-time broadcast fails
                // Log/handle if needed, but MediatR publication failures shouldn't abort successfully saved DB state
            }

            // 8️⃣ Trigger completion ONLY if batch is full
            if (batch.CurrentQuantity >= batch.TargetQuantity)
            {
                _jobDispatcher.Enqueue<IBatchCompletionJob>(
                    j => j.ExecuteAsync(batch.Id));
            }

            return new ConfirmJoinBatchResponse
            {
                Success = true
            };
        }
    }
}
