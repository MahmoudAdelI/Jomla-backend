using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Batches.Commands
{
    public class LeaveBatchCommandHandler : IRequestHandler<LeaveBatchCommand, LeaveBatchResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;

        public LeaveBatchCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
        }

        public async Task<LeaveBatchResponse> Handle(LeaveBatchCommand request, CancellationToken cancellationToken)
        {
            // 1️⃣ Fetch batch with participants
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

            // 3️⃣ Cancel Stripe hold
            var cancelResult = await _stripePaymentService.CancelPaymentAsync(
                participant.StripePaymentIntentId,
                cancellationToken: cancellationToken);

            if (!cancelResult.Success)
            {
                return new LeaveBatchResponse
                {
                    Success = false,
                    Error = $"Failed to cancel payment hold: {cancelResult.Error}"
                };
            }

            // 4️⃣ Mark as left
            participant.Status = BatchParticipantStatus.Left;

            // 5️⃣ Decrement batch quantity
            batch.CurrentQuantity -= participant.Quantity;

            // 6️⃣ Save changes
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

            // 7️⃣ Return response
            return new LeaveBatchResponse
            {
                Success = true,
                BatchId = request.BatchId,
                RemainingQuantity = batch.CurrentQuantity
            };
        }
    }
}