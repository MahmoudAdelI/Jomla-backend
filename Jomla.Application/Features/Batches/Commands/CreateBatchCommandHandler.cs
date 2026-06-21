using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Batches.Commands
{
    public class CreateBatchCommandHandler : IRequestHandler<CreateBatchCommand, CreateBatchResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IMediator _mediator;

        public CreateBatchCommandHandler(
            IAppDbContext context,
            IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<CreateBatchResponse> Handle(CreateBatchCommand request, CancellationToken cancellationToken)
        {

            // 1️⃣ Get Offer + Batches
            var offer = await _context.SupplierOffers
                .Include(o => o.Batches)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer == null)
                throw new NotFoundException(nameof(SupplierOffer), request.OfferId);

            // 2️⃣ Check Active
            if (offer.Status != SupplierOfferStatus.Active)
                throw new ConflictException($"Offer status is {offer.Status}. Cannot open batch.");

            // 3️⃣ Check no open batch
            var hasOpenBatch = offer.Batches.Any(b => b.Status == BatchStatus.Open);
            if (hasOpenBatch)
                throw new ConflictException("There is already an open batch.");

            // 4️⃣ Calculate remaining quantity
            var usedQuantity = offer.Batches
                .Where(b => b.Status != BatchStatus.Completed)
                .Sum(b => b.TargetQuantity);

            var remainingQuantity = offer.TotalQuantityAvailable - usedQuantity;

            if (remainingQuantity <= 0)
                throw new ConflictException("No quantity available.");

            // 5️⃣ لو الكمية أقل من المطلوب → Notification بس
            if (remainingQuantity < offer.BatchTargetQuantity)
            {
                var notification = new Notification
                { 
                    UserId = offer.SupplierId,
                    Type = NotificationType.BatchOpenedWithReducedQuantity,
                    Title = "Partial Batch Available",
                    Body = $"Only {remainingQuantity} units left. Do you want to open a partial batch?",
                    EntityId = offer.Id,
                    EntityType = nameof(SupplierOffer),
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync(cancellationToken);

                await _mediator.Publish(
                    new NotificationCreatedEvent(notification.UserId, notification.Id),
                    cancellationToken);

                return new CreateBatchResponse
                {
                    Success = false,
                    Message = "Remaining quantity is less than required. The supplier has been notified."
                };
            }

            // 6️⃣ نفتح Batch عادي
            var lastBatchNumber = offer.Batches.Max(b => (int?)b.BatchNumber) ?? 0;

            var batch = new SupplierBatch
            {
                OfferId = offer.Id,
                BatchNumber = lastBatchNumber + 1,
                TargetQuantity = offer.BatchTargetQuantity,
                CurrentQuantity = 0,
                Status = BatchStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            _context.SupplierBatches.Add(batch);

            //  Save مرة واحدة بس
            await _context.SaveChangesAsync(cancellationToken);

            return new CreateBatchResponse
            {
                Success = true,
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                TargetQuantity = batch.TargetQuantity,
                Message = $"Batch #{batch.BatchNumber} opened successfully."
            };
        }

    }
}