using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Batches.Commands
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
                throw new NotFoundException(nameof(SupplierBatch), request.BatchId);

            // 2️⃣ Validate batch is Open
            if (batch.Status != BatchStatus.Open)
                throw new ConflictException($"Batch is {batch.Status}. Cannot join.");

            // 3️⃣ Validate space available
            int spaceRemaining = batch.TargetQuantity - batch.CurrentQuantity;

            if (request.Quantity > spaceRemaining)
                throw new ConflictException($"Only {spaceRemaining} slots available.");

            // 4️⃣ Calculate total amount
            decimal totalAmount = request.Quantity * batch.Offer.UnitPrice;

            // 5️⃣ Create Stripe Payment Hold
            var paymentResult = await _stripePaymentService.CreatePaymentHoldAsync(
                request.BuyerId.ToString(),
                request.BuyerEmail,
                totalAmount,
                request.BatchId);

            if (!paymentResult.Success)
                throw new ConflictException($"Payment hold failed: {paymentResult.Error}");

            // 6️⃣ Create participant
            var participant = new BatchParticipant
            {
                BatchId = request.BatchId,
                BuyerId = request.BuyerId,
                Quantity = request.Quantity,
                StripePaymentIntentId = paymentResult.PaymentIntentId,
                Status = BatchParticipantStatus.Active,
                JoinedAt = DateTime.UtcNow
            };

            _context.BatchParticipants.Add(participant);

            // 7️⃣ Update batch quantity
            batch.CurrentQuantity += request.Quantity;

            // 8️⃣ Save changes
            await _context.SaveChangesAsync(cancellationToken);

            // 9️⃣ Trigger completion ONLY if batch is full
            if (batch.CurrentQuantity >= batch.TargetQuantity)
            {
                _jobDispatcher.Enqueue<IBatchCompletionJob>(
                    j => j.ExecuteAsync(batch.Id));
            }

            // 🔟 Return response
            return new JoinBatchResponse
            {
                Success = true,
                BatchId = request.BatchId,
                ParticipantQuantity = request.Quantity,
                TotalAmount = totalAmount,
                PaymentIntentId = paymentResult.PaymentIntentId,
                BatchCurrentQuantity = batch.CurrentQuantity,
                BatchTargetQuantity = batch.TargetQuantity
            };
        }
    }
}