using MediatR;
using System;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.DTOs;
using Jomla.Application.Features.Batches.Events;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Application.Common.Exceptions;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Commands
{
    public class ConfirmJoinBatchCommand : IRequest<bool>
    {
        public Guid BatchId { get; set; }
        public Guid BuyerId { get; set; }
        public int Quantity { get; set; }
        public string PaymentIntentId { get; set; } = null!;
    }

    public class ConfirmJoinBatchCommandHandler : IRequestHandler<ConfirmJoinBatchCommand, bool>
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

        public async Task<bool> Handle(ConfirmJoinBatchCommand request, CancellationToken cancellationToken)
        {
            // 1️⃣ Fetch batch with offer
            var batch = await _context.SupplierBatches
                .Include(b => b.Offer)
                .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

            if (batch == null)
                throw new NotFoundException(nameof(SupplierBatch), request.BatchId);

            // 2️⃣ Validate batch is Open
            if (batch.Status != BatchStatus.Open)
                throw new ConflictException($"Batch is {batch.Status}. Cannot join.");

            // 3️⃣ Check if already an ACTIVE participant
            var existingParticipant = await _context.BatchParticipants
                .FirstOrDefaultAsync(p => p.BatchId == request.BatchId
                                        && p.BuyerId == request.BuyerId, cancellationToken);

            if (existingParticipant != null && existingParticipant.Status == BatchParticipantStatus.Active)
                throw new ConflictException("You are already a participant in this batch.");

            // 3.5️⃣ Re-validate capacity
            int spaceRemaining = batch.TargetQuantity - batch.CurrentQuantity;
            if (request.Quantity > spaceRemaining)
            {
                // Rollback Stripe payment hold to prevent charging buyer
                await _stripePaymentService.CancelPaymentAsync(request.PaymentIntentId, cancellationToken);
                throw new ConflictException($"Only {spaceRemaining} slots available.");
            }

            // 4️⃣ Verify Stripe PaymentIntent status
            var paymentResult = await _stripePaymentService.GetPaymentIntentAsync(request.PaymentIntentId, cancellationToken);
            if (!paymentResult.Success)
                throw new BadRequestException($"Could not verify payment: {paymentResult.Error}");

            if (paymentResult.Status != "requires_capture" && paymentResult.Status != "succeeded")
                throw new BadRequestException($"Stripe payment intent status is '{paymentResult.Status}', but 'requires_capture' or 'succeeded' is required.");

            // 4.5️⃣ Verify Payment amount matches request.Quantity
            decimal expectedAmount = request.Quantity * batch.Offer.UnitPrice * (1 - batch.Offer.DiscountPercentage / 100m);
            long expectedAmountInCents = (long)Math.Round(expectedAmount * 100, MidpointRounding.AwayFromZero);

            if (paymentResult.Amount != expectedAmountInCents)
            {
                // Rollback Stripe payment hold
                await _stripePaymentService.CancelPaymentAsync(request.PaymentIntentId, cancellationToken);
                throw new BadRequestException("Payment amount does not match the requested quantity.");
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
                throw new ConflictException("The batch was updated by another request. Please try again.");
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
            }

            // 8️⃣ Trigger completion ONLY if batch is full
            if (batch.CurrentQuantity >= batch.TargetQuantity)
            {
                _jobDispatcher.Enqueue<IBatchCompletionJob>(
                    j => j.ExecuteAsync(batch.Id));
            }

            return true;
        }
    }
}
