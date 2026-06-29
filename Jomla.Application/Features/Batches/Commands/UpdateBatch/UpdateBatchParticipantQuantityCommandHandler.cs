using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.Commands.UpdateBatch;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Commands.UpdateBatch
{

    public class UpdateBatchParticipantQuantityCommandHandler : IRequestHandler<UpdateBatchParticipantQuantityCommand, UpdateBatchParticipantQuantityResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IBackgroundJobDispatcher _jobDispatcher;
        private readonly IMediator _mediator;

        public UpdateBatchParticipantQuantityCommandHandler(
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

        public async Task<UpdateBatchParticipantQuantityResponse> Handle(UpdateBatchParticipantQuantityCommand request, CancellationToken cancellationToken)
        {
            // 1️⃣ Fetch batch with offer
            var batch = await _context.SupplierBatches
                .Include(b => b.Offer)
                .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

            if (batch == null)
            {
                return new UpdateBatchParticipantQuantityResponse { Success = false, Error = "Batch not found." };
            }

            // 2️⃣ Validate batch is still open for modification
            if (batch.Status != BatchStatus.Open)
            {
                return new UpdateBatchParticipantQuantityResponse { Success = false, Error = $"Cannot edit quantity. Batch is {batch.Status}." };
            }

            // 3️⃣ Fetch existing participant
            var participant = await _context.BatchParticipants
                .FirstOrDefaultAsync(p => p.BatchId == request.BatchId && p.BuyerId == request.BuyerId, cancellationToken);

            if (participant == null || participant.Status != BatchParticipantStatus.Active)
            {
                return new UpdateBatchParticipantQuantityResponse { Success = false, Error = "You are not an active participant in this batch." };
            }

            // 4️⃣ Calculate quantity delta 
            int oldQuantity = participant.Quantity;
            int quantityDelta = request.NewQuantity - oldQuantity;

            if (quantityDelta == 0)
            {
                return new UpdateBatchParticipantQuantityResponse { Success = true, Error = "No changes made. Quantity is identical." };
            }

            // 5️⃣ Validate space capacity based on the delta
            int spaceRemaining = batch.TargetQuantity - batch.CurrentQuantity;
            if (quantityDelta > spaceRemaining)
            {
                return new UpdateBatchParticipantQuantityResponse { Success = false, Error = $"Insufficient slots. Only {spaceRemaining} slots left." };
            }

            // 6️⃣ Calculate full new amount
            decimal newTotalAmount = request.NewQuantity * batch.Offer.UnitPrice * (1 - batch.Offer.DiscountPercentage / 100m);

            // 7️⃣ Create NEW Stripe Payment Hold for the complete new amount
            var paymentResult = await _stripePaymentService.CreatePaymentHoldAsync(
                request.BuyerId.ToString(),
                request.BuyerEmail,
                newTotalAmount,
                request.BatchId,
                cancellationToken: cancellationToken);

            if (!paymentResult.Success)
            {
                return new UpdateBatchParticipantQuantityResponse { Success = false, Error = $"Stripe payment hold failed: {paymentResult.Error}" };
            }

            // Backup old intent to release it later
            string oldPaymentIntentId = participant.StripePaymentIntentId;

            // 8️⃣ Apply modifications to entities
            participant.Quantity = request.NewQuantity;
            participant.StripePaymentIntentId = paymentResult.PaymentIntentId;
            participant.JoinedAt = DateTime.UtcNow;

            batch.CurrentQuantity += quantityDelta; // تزعيل أو تقليل إجمالي الباتش بناءً على الإشارة (+ أو -)

            // 9️⃣ Save to database and safely cycle Stripe holds
            try
            {
                await _context.SaveChangesAsync(cancellationToken);

                // 9.5️⃣ Publish batch update event
                try
                {
                    var updateDto = Jomla.Application.Features.Batches.DTOs.BatchUpdatedDto.MapFrom(batch);
                    await _mediator.Publish(new Jomla.Application.Features.Batches.Events.BatchUpdatedEvent(batch.OfferId, updateDto), cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log but don't abort
                }

                // Release old Stripe payment hold since the new one is safely recorded
                if (!string.IsNullOrEmpty(oldPaymentIntentId))
                {
                    await _stripePaymentService.CancelPaymentAsync(oldPaymentIntentId, cancellationToken);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                // Rollback new hold if db fails
                await _stripePaymentService.CancelPaymentAsync(paymentResult.PaymentIntentId, cancellationToken);
                return new UpdateBatchParticipantQuantityResponse { Success = false, Error = "Concurrency conflict occurred. Please retry." };
            }

            // 🔟 Check fulfillment trigger
            if (batch.CurrentQuantity >= batch.TargetQuantity)
            {
                _jobDispatcher.Enqueue<IBatchCompletionJob>(j => j.ExecuteAsync(batch.Id));
            }

            return new UpdateBatchParticipantQuantityResponse
            {
                Success = true,
                UpdatedQuantity = request.NewQuantity,
                NewTotalAmount = newTotalAmount,
                NewPaymentIntentId = paymentResult.PaymentIntentId,
                ClientSecret = paymentResult.ClientSecret,
                BatchCurrentQuantity = batch.CurrentQuantity
            };
        }
    }
}