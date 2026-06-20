using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.Commands.OpenBatch;
using Jomla.Application.Features.Notifications;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Batches.Commands.CompleteBatch
{
    public class CompleteBatchCommandHandler : IRequestHandler<CompleteBatchCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IMediator _mediator;

        public CompleteBatchCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            IMediator mediator)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _mediator = mediator;
        }

        public async Task Handle(CompleteBatchCommand request, CancellationToken cancellationToken)
        {
            // Step 1: Fetch batch
            var batch = await _context.SupplierBatches
                .Include(b => b.Offer)
                .Include(b => b.Participants)
                .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken);

            if (batch == null) return;

            // Step 2: Idempotency check
            if (batch.Status == BatchStatus.Completed)
            {
                var hasFailedOrders = await _context.Orders
                    .AnyAsync(o => o.BatchId == batch.Id && o.Status == OrderStatus.Failed, cancellationToken);

                if (!hasFailedOrders)
                    return;
            }
            else
            {
                // Step 3: Lock batch immediately
                batch.Status = BatchStatus.Completed;
                batch.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Step 4: Resilient capture loop
            var activeParticipants = batch.Participants
                .Where(p => p.Status == BatchParticipantStatus.Active)
                .ToList();

            bool hasFailure = false;
            var successfulBuyerIds = new List<Guid>();

            foreach (var participant in activeParticipants)
            {
                try
                {
                    var existingOrder = await _context.Orders
                        .FirstOrDefaultAsync(o => o.BatchId == batch.Id && o.BuyerId == participant.BuyerId, cancellationToken);

                    if (existingOrder != null && existingOrder.Status == OrderStatus.Paid)
                        continue;

                    var captureResult = await _stripePaymentService.CapturePaymentAsync(participant.StripePaymentIntentId);

                    if (existingOrder != null)
                    {
                        existingOrder.Status = captureResult.Success ? OrderStatus.Paid : OrderStatus.Failed;
                        existingOrder.PaidAt = captureResult.Success ? DateTime.UtcNow : null;
                    }
                    else
                    {
                        var order = new Order
                        {
                            BuyerId = participant.BuyerId,
                            BatchId = batch.Id,
                            OfferId = null,
                            Quantity = participant.Quantity,
                            TotalAmount = participant.Quantity * batch.Offer.UnitPrice * (1 - batch.Offer.DiscountPercentage / 100m),
                            Status = captureResult.Success ? OrderStatus.Paid : OrderStatus.Failed,
                            PaidAt = captureResult.Success ? DateTime.UtcNow : null,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Orders.Add(order);
                    }

                    await _context.SaveChangesAsync(cancellationToken);

                    if (captureResult.Success)
                        successfulBuyerIds.Add(participant.BuyerId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Capture failed for buyer {participant.BuyerId}: {ex.Message}");
                    hasFailure = true;
                }
            }

            if (hasFailure)
                throw new Exception("One or more captures failed. Hangfire will retry.");

            // Step 5: Open next batch
            await _mediator.Send(new OpenBatchCommand(batch.OfferId), cancellationToken);

            // Step 6: Send notifications
            foreach (var buyerId in successfulBuyerIds)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = buyerId,
                    Type = NotificationType.BatchCompleted,
                    Title = "Batch Completed",
                    Body = "Your payment was captured and your order has been created successfully.",
                    EntityId = batch.Id,
                    EntityType = "SupplierBatch",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync(cancellationToken);

                await _mediator.Publish(new NotificationCreatedEvent(buyerId, notification.Id), cancellationToken);
            }
        }
    }
}
