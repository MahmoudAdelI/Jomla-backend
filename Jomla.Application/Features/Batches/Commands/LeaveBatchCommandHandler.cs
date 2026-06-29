using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jomla.Application.Features.Batches.Commands
{
    public class LeaveBatchCommandHandler : IRequestHandler<LeaveBatchCommand, LeaveBatchResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly ILogger<LeaveBatchCommandHandler> _logger;
        private readonly IMediator _mediator;

        public LeaveBatchCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            ILogger<LeaveBatchCommandHandler> logger,
            IMediator mediator)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<LeaveBatchResponse> Handle(LeaveBatchCommand request, CancellationToken cancellationToken)
        {
            // 1️⃣ Fetch batch with participants and offer (for OfferId mapping)
            var batch = await _context.SupplierBatches
                .Include(b => b.Participants)
                .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

            if (batch == null)
            {
                return new LeaveBatchResponse
                {
                    Success = false,
                    Error = $"SupplierBatch with ID '{request.BatchId}' was not found."
                };
            }

            // Validate batch is Open
            if (batch.Status != BatchStatus.Open)
            {
                return new LeaveBatchResponse
                {
                    Success = false,
                    Error = $"Cannot leave a batch that is {batch.Status}."
                };
            }

            // 2️⃣ Find participant
            var participant = batch.Participants
                .FirstOrDefault(p =>
                    p.BuyerId == request.BuyerId &&
                    p.Status == BatchParticipantStatus.Active);

            if (participant == null)
            {
                return new LeaveBatchResponse
                {
                    Success = false,
                    Error = "You are not an active participant in this batch."
                };
            }

            // 3️⃣ Save the payment intent ID before modifying the entity
            var paymentIntentId = participant.StripePaymentIntentId;

            // 4️⃣ Mark as left
            participant.Status = BatchParticipantStatus.Left;

            // 5️⃣ Decrement batch quantity
            batch.CurrentQuantity -= participant.Quantity;

            // 6️⃣ Save changes first - if concurrency fails, Stripe hold remains intact (safe)
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new LeaveBatchResponse
                {
                    Success = false,
                    Error = "The batch was updated by another request. Please try again."
                };
            }

            // 6.5️⃣ Publish batch update event
            try
            {
                var updateDto = Jomla.Application.Features.Batches.DTOs.BatchUpdatedDto.MapFrom(batch);
                await _mediator.Publish(new Jomla.Application.Features.Batches.Events.BatchUpdatedEvent(batch.OfferId, updateDto), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish BatchUpdatedEvent in LeaveBatchCommandHandler.");
            }

            // 7️⃣ Cancel Stripe hold AFTER successful DB commit
            // If this fails, the hold expires naturally (7 days) and buyer won't be charged
            // since they are already marked as Left.
            try
            {
                var cancelResult = await _stripePaymentService.CancelPaymentAsync(
                    paymentIntentId,
                    cancellationToken: cancellationToken);

                if (!cancelResult.Success)
                {
                    _logger.LogWarning("Failed to cancel payment hold for payment intent {PaymentIntentId} on leave batch {BatchId}: {Error}",
                        paymentIntentId, request.BatchId, cancelResult.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cancelling payment hold for payment intent {PaymentIntentId} on leave batch {BatchId}",
                    paymentIntentId, request.BatchId);
            }

            // 8️⃣ Return response
            return new LeaveBatchResponse
            {
                Success = true,
                BatchId = request.BatchId,
                RemainingQuantity = batch.CurrentQuantity
            };
        }
    }
}