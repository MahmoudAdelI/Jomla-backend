using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using Jomla.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;

namespace Jomla.Application.Features.GroupRequests.Commands.AcceptGroupRequestOffer
{
    public class AcceptGroupRequestOfferCommandHandler(
        IAppDbContext context,
        IStripePaymentService stripePaymentService,
        IMediator mediator,
        IBackgroundJobDispatcher jobDispatcher,
        ILogger<AcceptGroupRequestOfferCommandHandler> logger) : IRequestHandler<AcceptGroupRequestOfferCommand, AcceptGroupRequestOfferResponse>
    {
        private readonly IAppDbContext _context = context;
        private readonly IStripePaymentService _stripePaymentService = stripePaymentService;
        private readonly IMediator _mediator = mediator;
        private readonly IBackgroundJobDispatcher _jobDispatcher = jobDispatcher;
        private readonly ILogger<AcceptGroupRequestOfferCommandHandler> _logger = logger;

        public async Task<AcceptGroupRequestOfferResponse> Handle(
            AcceptGroupRequestOfferCommand request,
            CancellationToken cancellationToken)
        {
            
            // Step 1: Fetch offer with group request and participants
            var offer = await _context.GroupRequestOffers
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .Include(o => o.Responses)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer == null)
                return new AcceptGroupRequestOfferResponse { Success = false, Error = "Offer not found" };

            // Step 2: Validate offer is Open
            if (offer.Status != GroupRequestOfferStatus.Open)
                return new AcceptGroupRequestOfferResponse { Success = false, Error = $"Offer is {offer.Status}" };

            // Step 3: Validate group request is active
            if (offer.GroupRequest.Status != GroupRequestStatus.Active)
                return new AcceptGroupRequestOfferResponse { Success = false, Error = "Group request is not active" };

            // Step 4: Find buyer participant
            var participant = offer.GroupRequest.Participants
                .FirstOrDefault(p => p.BuyerId == request.BuyerId && p.Status == GroupRequestParticipantStatus.Active);

            if (participant == null)
                return new AcceptGroupRequestOfferResponse { Success = false, Error = "You are not an active participant in this group request" };

            // Step 5: Check if already accepted
            var existingResponse = offer.Responses
                .FirstOrDefault(r => r.BuyerId == request.BuyerId);

            if (existingResponse != null && existingResponse.Response == BuyerOfferResponseType.Accepted)
                return new AcceptGroupRequestOfferResponse { Success = false, Error = "You have already accepted this offer" };

            // Validate AcceptedQuantity bounds
            int remaining = offer.QuantityAvailable - offer.AcceptedQuantity;

            if (request.AcceptedQuantity <= 0)
                return new AcceptGroupRequestOfferResponse { Success = false, Error = "Accepted quantity must be at least 1." };

            if (request.AcceptedQuantity > remaining)
                return new AcceptGroupRequestOfferResponse { Success = false, Error = $"Only {remaining} slot(s) remaining. You cannot accept more than that." };

            var currentAcceptedQuantity = offer.AcceptedQuantity;

            // Calculate payment amount using the buyer-chosen accepted quantity
            decimal totalAmount = request.AcceptedQuantity * offer.CurrentUnitPrice;

            // Step 7: Create Stripe payment hold
            var paymentResult = await _stripePaymentService.CreatePaymentHoldAsync(
                request.BuyerId.ToString(),
                request.BuyerEmail,
                totalAmount,
                request.OfferId);

            if (!paymentResult.Success)
            {
                _logger.LogWarning("Payment hold failed for buyer {BuyerId} on offer {OfferId}: {Error}",
                    request.BuyerId, request.OfferId, paymentResult.Error);
                return new AcceptGroupRequestOfferResponse { Success = false, Error = "Payment hold failed" };
            }

            //  FIX 2: Wrap DB operations in try-catch to cancel payment hold if DB fails
            try
            {
                // Step 8: Create or update buyer response
                if (existingResponse == null)
                {
                    existingResponse = new BuyerOfferResponse
                    {
                        OfferId = request.OfferId,
                        BuyerId = request.BuyerId,
                        Response = BuyerOfferResponseType.Accepted,
                        AcceptedQuantity = request.AcceptedQuantity,
                        StripePaymentIntentId = paymentResult.PaymentIntentId,
                        RespondedAt = DateTime.UtcNow
                    };
                    _context.BuyerOfferResponses.Add(existingResponse);
                    offer.Responses.Add(existingResponse);
                }
                else
                {
                    existingResponse.Response = BuyerOfferResponseType.Accepted;
                    existingResponse.AcceptedQuantity = request.AcceptedQuantity;
                    existingResponse.StripePaymentIntentId = paymentResult.PaymentIntentId;
                    existingResponse.RespondedAt = DateTime.UtcNow;
                }

                // Update the persisted AcceptedQuantity counter on the offer
                offer.AcceptedQuantity = currentAcceptedQuantity + request.AcceptedQuantity;

                // Step 9: Save Changes
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save buyer offer response for buyer {BuyerId}. Cancelling payment hold {PaymentIntentId}",
                    request.BuyerId, paymentResult.PaymentIntentId);

                // Revert/Cancel Stripe Payment Hold if DB save failed
                await _stripePaymentService.CancelPaymentAsync(paymentResult.PaymentIntentId);

                return new AcceptGroupRequestOfferResponse { Success = false, Error = "An error occurred while saving your response. Payment hold was released." };
            }

            // Step 10: Calculate updated total accepted quantity
            var updatedAcceptedQuantity = offer.AcceptedQuantity;

            // Step 11: Check if offer is complete
            bool isComplete = updatedAcceptedQuantity >= offer.QuantityAvailable;

            if (isComplete)
            {
                // Offload to background job — the accepting buyer's request must not block
                // on all other buyers' Stripe captures completing synchronously.
                _jobDispatcher.Enqueue<IGroupRequestOfferFillJob>(j => j.ExecuteAsync(request.OfferId));
            }

            return new AcceptGroupRequestOfferResponse
            {
                Success = true,
                OfferId = request.OfferId,
                GroupRequestId = offer.GroupRequestId,
                PaymentIntentId = paymentResult.PaymentIntentId,
                AcceptedQuantity = updatedAcceptedQuantity,
                TotalAmount = totalAmount,
                Message = isComplete
                    ? "Offer accepted! All slots filled, processing..."
                    : $"Offer accepted! {updatedAcceptedQuantity}/{offer.QuantityAvailable} slots filled"
            };
        }
    }
}
