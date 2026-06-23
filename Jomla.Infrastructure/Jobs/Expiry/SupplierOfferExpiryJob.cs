using Jomla.Application.Features.Batches.Commands.CompleteBatch;
using Jomla.Application.Features.Batches.Commands.FailBatch;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Jobs.Expiry;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Infrastructure.Persistance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Infrastructure.Jobs.Expiry
{
    public class SupplierOfferExpiryJob(
        IDbContextFactory<AppDbContext> contextFactory,
        IMediator mediator) : ISupplierOfferExpiryJob
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;
        private readonly IMediator _mediator = mediator;
        public async Task ExecuteAsync(Guid offerId, CancellationToken ct)
        {
            await using var db = await _contextFactory.CreateDbContextAsync(ct);

            var offer = await db.SupplierOffers
                .FirstOrDefaultAsync(o => o.Id == offerId, ct);
            if (offer is null) return;

            if (offer.Status == SupplierOfferStatus.Expired) return;

            var batch = await db.SupplierBatches
                .Include(b => b.Participants)
                .FirstOrDefaultAsync(b => b.OfferId == offerId && b.Status == BatchStatus.Open, ct);

            // No open batch — nothing to settle, just expire the offer
            if (batch is null)
            {
                offer.Status = SupplierOfferStatus.Expired;
                await db.SaveChangesAsync(ct);
                return;
            }

            var shouldCapture = 
                offer.MinFallbackQuantity.HasValue
                && batch.CurrentQuantity >= offer.MinFallbackQuantity.Value;

            if (shouldCapture)
            {
                // complete batch
                await _mediator.Send(new CompleteBatchCommand(batch.Id), ct);

                // Reload the offer to get the updated RowVersion after CompleteBatchCommand modified it
                await db.Entry(offer).ReloadAsync(ct);
            }
            else
            {
                // fail batch
                await _mediator.Send(new FailBatchCommand(batch.Id), ct);
            }

            offer.Status = SupplierOfferStatus.Expired;

            var notification = new Notification
            {
                UserId = offer.SupplierId,
                Type = NotificationType.OfferExpired,
                Title = shouldCapture ? "Your offer expired and was fulfilled" : "Your offer expired",
                Body = shouldCapture
                ? $"The last batch reached the fallback threshold. {batch.CurrentQuantity} units were captured."
                : "The last batch did not meet the fallback threshold. All holds have been cancelled.",
                EntityId = offer.Id,
                EntityType = nameof(SupplierOffer),
                IsRead = false
            };

            db.Notifications.Add(notification);
            await db.SaveChangesAsync(ct);

            await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), ct);
        }
    }
}
