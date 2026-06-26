using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.Commands.CompleteBatch;
using Jomla.Application.Features.Batches.Commands.FailBatch;
using Jomla.Application.Features.Notifications;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Offers.Commands.ExpireSupplierOffer
{
    public class ExpireSupplierOfferCommandHandler(
        IAppDbContext db,
        IMediator mediator) : IRequestHandler<ExpireSupplierOfferCommand>
    {
        private readonly IAppDbContext _db = db;
        private readonly IMediator _mediator = mediator;

        public async Task Handle(ExpireSupplierOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _db.SupplierOffers
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);
            if (offer is null) return;

            if (offer.Status == SupplierOfferStatus.Expired) return;
            if (offer.Status != SupplierOfferStatus.Active) return;

            var batch = await _db.SupplierBatches
                .Include(b => b.Participants)
                .FirstOrDefaultAsync(b => b.OfferId == request.OfferId && b.Status == BatchStatus.Open, cancellationToken);

            // No open batch — nothing to settle, just expire the offer
            if (batch is null)
            {
                offer.Status = SupplierOfferStatus.Expired;
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            var shouldCapture = 
                offer.MinFallbackQuantity.HasValue
                && batch.CurrentQuantity >= offer.MinFallbackQuantity.Value;

            offer.Status = SupplierOfferStatus.Expired;
            await _db.SaveChangesAsync(cancellationToken);

            if (shouldCapture)
            {
                // complete batch
                await _mediator.Send(new CompleteBatchCommand(batch.Id), cancellationToken);
            }
            else
            {
                // fail batch
                await _mediator.Send(new FailBatchCommand(batch.Id), cancellationToken);
            }

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

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), cancellationToken);
        }
    }
}
