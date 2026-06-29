using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.Commands.OpenBatch;
using Jomla.Application.Features.Batches.DTOs;
using Jomla.Application.Features.Batches.Events;
using Jomla.Application.Features.Notifications;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jomla.Application.Features.Batches.Commands.CompleteBatch
{
    public class CompleteBatchCommandHandler : IRequestHandler<CompleteBatchCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IMediator _mediator;
        private readonly ILogger<CompleteBatchCommandHandler> _logger;

        public CompleteBatchCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            IMediator mediator,
            ILogger<CompleteBatchCommandHandler> logger)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(CompleteBatchCommand request, CancellationToken cancellationToken)
        {
            // Step 1: Fetch batch with offer and participants
            var batch = await _context.SupplierBatches
                .Include(b => b.Offer)
                .Include(b => b.Participants)
                .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

            if (batch == null) return;

            // Step 2: Idempotency check - if already completed, only retry failed orders
            // Bug #4 Fix: filter out Left participants when checking for failed orders on retry
            if (batch.Status == BatchStatus.Completed)
            {
                var activeBuyerIds = batch.Participants
                    .Where(p => p.Status == BatchParticipantStatus.Active)
                    .Select(p => p.BuyerId)
                    .ToList();

                var hasFailedOrders = await _context.Orders
                    .AnyAsync(o => o.BatchId == batch.Id
                                && o.Status == OrderStatus.Failed
                                && activeBuyerIds.Contains(o.BuyerId),
                        cancellationToken);

                if (!hasFailedOrders)
                    return;
            }
            else
            {
                // Step 3: Lock batch immediately to prevent concurrent processing
                batch.Status = BatchStatus.Completed;
                batch.CompletedAt = DateTime.UtcNow;
                batch.Offer.TotalQuantityAvailable -= batch.CurrentQuantity;

                if (batch.Offer.Status == SupplierOfferStatus.Active && batch.Offer.TotalQuantityAvailable <= 0)
                    batch.Offer.Status = SupplierOfferStatus.Inactive;

                await _context.SaveChangesAsync(cancellationToken);
            }

            // Step 4: Resilient capture loop - each participant captured and saved individually
            var activeParticipants = batch.Participants
                .Where(p => p.Status == BatchParticipantStatus.Active)
                .ToList();

            bool hasFailure = false;
            var successfulBuyerIds = new List<Guid>();

            foreach (var participant in activeParticipants)
            {
                try
                {
                    // Skip if already paid (retry safety)
                    var existingOrder = await _context.Orders
                        .FirstOrDefaultAsync(o => o.BatchId == batch.Id && o.BuyerId == participant.BuyerId, cancellationToken);

                    if (existingOrder != null && existingOrder.Status == OrderStatus.Paid)
                        continue;

                    // Capture payment from Stripe with idempotency key to prevent double charging on retry
                    var captureResult = await _stripePaymentService.CapturePaymentAsync(
                        participant.StripePaymentIntentId,
                        idempotencyKey: $"capture-{request.BatchId}-{participant.BuyerId}",
                        cancellationToken: cancellationToken);

                    if (existingOrder != null)
                    {
                        // Update existing failed order
                        existingOrder.Status = captureResult.Success ? OrderStatus.Paid : OrderStatus.Failed;
                        existingOrder.PaidAt = captureResult.Success ? DateTime.UtcNow : null;
                    }
                    else
                    {
                        // Bug #2 Fix: OfferId was hardcoded to null - use batch.OfferId
                        _context.Orders.Add(new Order
                        {
                            BuyerId = participant.BuyerId,
                            BatchId = batch.Id,
                            OfferId = batch.OfferId,
                            Quantity = participant.Quantity,
                            TotalAmount = participant.Quantity * batch.Offer.UnitPrice * (1 - batch.Offer.DiscountPercentage / 100m),
                            Status = captureResult.Success ? OrderStatus.Paid : OrderStatus.Failed,
                            PaidAt = captureResult.Success ? DateTime.UtcNow : null,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // Save each result immediately so retries can resume from where they stopped
                    await _context.SaveChangesAsync(cancellationToken);

                    if (captureResult.Success)
                        successfulBuyerIds.Add(participant.BuyerId);
                    else
                        hasFailure = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Capture failed for buyer {BuyerId} in batch {BatchId}",
                        participant.BuyerId, batch.Id);
                    hasFailure = true;
                }
            }

            // Bug #3 Fix: moved throw to AFTER Step 5 and Step 6 so that:
            // - The next batch is always opened even if some individual captures failed
            // - Successful buyers and the supplier always receive their notifications

            // Step 5: Open next batch for the same offer
            await _mediator.Send(new OpenBatchCommand(batch.OfferId), cancellationToken);

            // Step 6: Send notifications in bulk - single DB round trip
            var notifications = new List<Notification>();

            // Buyer notifications
            notifications.AddRange(successfulBuyerIds.Select(buyerId => new Notification
            {
                UserId = buyerId,
                Type = NotificationType.BatchCompleted,
                Title = "Batch Completed",
                Body = "Your payment was captured and your order has been created successfully.",
                EntityId = batch.Id,
                EntityType = "SupplierBatch",
                IsRead = false
            }));

            // Bug #1 Fix: supplier notification was missing entirely
            notifications.Add(new Notification
            {
                UserId = batch.Offer.SupplierId,
                Type = NotificationType.BatchCompleted,
                Title = $"Batch #{batch.BatchNumber} Completed",
                Body = $"Batch #{batch.BatchNumber} for your offer \"{batch.Offer.Title}\" has been completed with {batch.CurrentQuantity} unit(s).",
                EntityId = batch.Id,
                EntityType = "SupplierBatch",
                IsRead = false
            });

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync(cancellationToken);

            // Fetch the newly opened batch if any and publish batch completion/new-batch updates
            try
            {
                var newBatch = await _context.SupplierBatches
                    .FirstOrDefaultAsync(b => b.OfferId == batch.OfferId && b.Status == BatchStatus.Open, cancellationToken);
                var newBatchDto = newBatch is not null ? BatchUpdatedDto.MapFrom(newBatch) : null;

                var completedBatchUpdate = BatchUpdatedDto.MapFrom(batch, newBatchDto);
                await _mediator.Publish(new BatchUpdatedEvent(batch.OfferId, completedBatchUpdate), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish BatchUpdatedEvent in CompleteBatchCommandHandler.");
            }

            // Fire real-time event for each notification - failure here does not affect financials
            foreach (var notification in notifications)
            {
                try
                {
                    await _mediator.Publish(
                        new NotificationCreatedEvent(notification.UserId, notification.Id),
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Real-time notification failed for user {UserId} - financials unaffected",
                        notification.UserId);
                }
            }

            // If any capture failed, throw now so Hangfire retries the failed orders
            // next-batch-open and successful notifications have already been persisted above
            if (hasFailure)
                throw new Exception("One or more captures failed. Hangfire will retry.");
        }
    }
}
