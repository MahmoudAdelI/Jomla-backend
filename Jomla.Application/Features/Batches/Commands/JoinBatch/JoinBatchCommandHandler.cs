using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Commands.JoinBatch
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
            // 1️⃣ Fetch batch with offer
            var batch = await _context.SupplierBatches
                .Include(b => b.Offer)
                .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

            if (batch == null)
            {
                return new JoinBatchResponse
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
                return new JoinBatchResponse
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
                return new JoinBatchResponse
                {
                    Success = false,
                    Error = "You are already a participant in this batch. Please use the Edit option to update your quantity.",
                    ErrorCode = "ALREADY_PARTICIPANT",
                    StatusCode = 400
                };
            }

            // 4️⃣ Validate space available
            int spaceRemaining = batch.TargetQuantity - batch.CurrentQuantity;

            if (request.Quantity > spaceRemaining)
            {
                return new JoinBatchResponse
                {
                    Success = false,
                    Error = $"Only {spaceRemaining} slots available.",
                    ErrorCode = "INSUFFICIENT_SLOTS",
                    SlotsAvailable = spaceRemaining,
                    StatusCode = 409
                };
            }

            // 5️⃣ Calculate total amount
            decimal totalAmount = request.Quantity * batch.Offer.UnitPrice * (1 - batch.Offer.DiscountPercentage / 100m);

            // 6️⃣ Create Stripe Payment Hold
            var paymentResult = await _stripePaymentService.CreatePaymentHoldAsync(
                request.BuyerId.ToString(),
                request.BuyerEmail,
                totalAmount,
                request.BatchId,
                cancellationToken: cancellationToken);

            if (!paymentResult.Success)
            {
                return new JoinBatchResponse
                {
                    Success = false,
                    Error = $"Payment hold failed: {paymentResult.Error}",
                    ErrorCode = paymentResult.ErrorCode ?? "PAYMENT_HOLD_FAILED",
                    StatusCode = 409
                };
            }

            // 7️⃣ Return response for client-side Stripe confirmation
            return new JoinBatchResponse
            {
                Success = true,
                BatchId = request.BatchId,
                ParticipantQuantity = request.Quantity,
                TotalAmount = totalAmount,
                PaymentIntentId = paymentResult.PaymentIntentId,
                ClientSecret = paymentResult.ClientSecret,
                BatchCurrentQuantity = batch.CurrentQuantity,
                BatchTargetQuantity = batch.TargetQuantity
            };
        }
        //    public async Task<JoinBatchResponse> Handle(JoinBatchCommand request, CancellationToken cancellationToken)
        //    {
        //        // 1️⃣ Fetch batch with offer
        //        var batch = await _context.SupplierBatches
        //            .Include(b => b.Offer)
        //            .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

        //        if (batch == null)
        //        {
        //            return new JoinBatchResponse
        //            {
        //                Success = false,
        //                Error = $"SupplierBatch with ID '{request.BatchId}' was not found.",
        //                ErrorCode = "NOT_FOUND",
        //                StatusCode = 404
        //            };
        //        }

        //        // 2️⃣ Validate batch is Open
        //        if (batch.Status != BatchStatus.Open)
        //        {
        //            return new JoinBatchResponse
        //            {
        //                Success = false,
        //                Error = $"Batch is {batch.Status}. Cannot join.",
        //                ErrorCode = "INVALID_BATCH_STATUS",
        //                StatusCode = 409
        //            };
        //        }


        //        var existingParticipantId = await _context.BatchParticipants
        //            .Where(p => p.BatchId == request.BatchId && p.BuyerId == request.BuyerId)
        //            .Select(p => p.BatchId) // هيرجع الـ Id بس كـ Guid أو int
        //            .FirstOrDefaultAsync(cancellationToken);

        //        if (existingParticipantId != default) // لو لقى له Id يبقى موجود فعلاً في الباتش ده
        //        {
        //            return new JoinBatchResponse
        //            {
        //                Success = false,
        //                Error = "You are already a participant in this batch. Please use the Edit option to update your quantity.",
        //                ErrorCode = "ALREADY_PARTICIPANT",
        //                StatusCode = 400
        //            };
        //        }

        //        // 4️⃣ Validate space available
        //        int spaceRemaining = batch.TargetQuantity - batch.CurrentQuantity;

        //        if (request.Quantity > spaceRemaining)
        //        {
        //            return new JoinBatchResponse
        //            {
        //                Success = false,
        //                Error = $"Only {spaceRemaining} slots available.",
        //                ErrorCode = "INSUFFICIENT_SLOTS",
        //                SlotsAvailable = spaceRemaining,
        //                StatusCode = 409
        //            };
        //        }

        //        // 5️⃣ Calculate total amount
        //        decimal totalAmount = request.Quantity * batch.Offer.UnitPrice * (1 - batch.Offer.DiscountPercentage / 100m);

        //        // 6️⃣ Create Stripe Payment Hold
        //        var paymentResult = await _stripePaymentService.CreatePaymentHoldAsync(
        //            request.BuyerId.ToString(),
        //            request.BuyerEmail,
        //            totalAmount,
        //            request.BatchId,
        //            cancellationToken: cancellationToken);

        //        if (!paymentResult.Success)
        //        {
        //            return new JoinBatchResponse
        //            {
        //                Success = false,
        //                Error = $"Payment hold failed: {paymentResult.Error}",
        //                ErrorCode = paymentResult.ErrorCode ?? "PAYMENT_HOLD_FAILED",
        //                StatusCode = 409
        //            };
        //        }

        //        // 7️⃣ Create new participant 
        //        var participant = new BatchParticipant
        //        {
        //            BatchId = request.BatchId,
        //            BuyerId = request.BuyerId,
        //            Quantity = request.Quantity,
        //            StripePaymentIntentId = paymentResult.PaymentIntentId,
        //            Status =BatchParticipantStatus.Active,
        //            JoinedAt = DateTime.UtcNow
        //        };
        //        _context.BatchParticipants.Add(participant);

        //        // 8️⃣ Update batch quantity
        //        batch.CurrentQuantity += request.Quantity;

        //        // 9️⃣ Save changes
        //        try
        //        {
        //            await _context.SaveChangesAsync(cancellationToken);
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            // Rollback Stripe payment hold to prevent charging buyer when save fails
        //            await _stripePaymentService.CancelPaymentAsync(paymentResult.PaymentIntentId, cancellationToken);

        //            return new JoinBatchResponse
        //            {
        //                Success = false,
        //                Error = "The batch was updated by another request. Please try again.",
        //                ErrorCode = "CONCURRENCY_CONFLICT",
        //                StatusCode = 409
        //            };
        //        }

        //        // 🔟 Trigger completion ONLY if batch is full
        //        if (batch.CurrentQuantity >= batch.TargetQuantity)
        //        {
        //            _jobDispatcher.Enqueue<IBatchCompletionJob>(
        //                j => j.ExecuteAsync(batch.Id));
        //        }

        //        // 1️⃣1️⃣ Return response
        //        return new JoinBatchResponse
        //        {
        //            Success = true,
        //            BatchId = request.BatchId,
        //            ParticipantQuantity = request.Quantity,
        //            TotalAmount = totalAmount,
        //            PaymentIntentId = paymentResult.PaymentIntentId,
        //            ClientSecret = paymentResult.ClientSecret,
        //            BatchCurrentQuantity = batch.CurrentQuantity,
        //            BatchTargetQuantity = batch.TargetQuantity
        //        };
        //    }
        //}
    }
}