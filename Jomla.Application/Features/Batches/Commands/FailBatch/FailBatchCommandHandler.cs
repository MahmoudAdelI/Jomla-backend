using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.DTOs;
using Jomla.Application.Features.Batches.Events;
using Jomla.Application.Features.Notifications;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jomla.Application.Features.Batches.Commands.FailBatch
{
    public class FailBatchCommandHandler(
        IAppDbContext db,
        IStripePaymentService stripe,
        IMediator mediator,
        ILogger<FailBatchCommandHandler> logger
        ) : IRequestHandler<FailBatchCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly IStripePaymentService _stripe = stripe;
        private readonly IMediator _mediator = mediator;
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
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                    "Failed to cancel Stripe hold for buyer {BuyerId} on batch {BatchId}. PaymentIntentId: {PaymentIntentId}",
                    participant.BuyerId, request.BatchId, participant.StripePaymentIntentId);
                }

                participant.Status = BatchParticipantStatus.Left;
            }

            batch.Status = BatchStatus.Failed;
            batch.CompletedAt = DateTime.UtcNow;

            // Persist batch + participant changes
            await _db.SaveChangesAsync(cancellationToken);

            // Notify each affected buyer
            var notifications = new List<Notification>();
            foreach (var participant in batch.Participants)
            {
                var notification = new Notification
                {
                    UserId = participant.BuyerId,
                    Type = NotificationType.BatchCanceledBySupplier,
                    Title = "Batch canceled by supplier",
                    Body = "The supplier has deactivated the offer. Your payment hold has been released.",
                    EntityId = batch.Id,
                    EntityType = nameof(SupplierBatch),
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                notifications.Add(notification);
                _db.Notifications.Add(notification);
            }

            if (notifications.Count > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);

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
                            "Failed to push notification to buyer {BuyerId} for failed batch {BatchId}.",
                            notification.UserId, batch.Id);
                    }
                }
            }

            // Broadcast batch status update over SignalR
            try
            {
                var batchDto = BatchUpdatedDto.MapFrom(batch);
                await _mediator.Publish(new BatchUpdatedEvent(batch.OfferId, batchDto), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to publish BatchUpdatedEvent for failed batch {BatchId}.", batch.Id);
            }
        }
    }
}
