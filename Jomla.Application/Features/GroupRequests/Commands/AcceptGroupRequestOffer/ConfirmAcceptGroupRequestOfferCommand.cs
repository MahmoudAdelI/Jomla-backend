using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using Jomla.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.AcceptGroupRequestOffer
{
    public record ConfirmAcceptGroupRequestOfferCommand(
        Guid OfferId,
        Guid BuyerId,
        string BuyerEmail,
        int AcceptedQuantity,
        string PaymentIntentId
    ) : IRequest<ConfirmAcceptGroupRequestOfferResponse>;

    public class ConfirmAcceptGroupRequestOfferResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? ErrorCode { get; set; }
        public int? StatusCode { get; set; }
    }

    public class ConfirmAcceptGroupRequestOfferCommandHandler : IRequestHandler<ConfirmAcceptGroupRequestOfferCommand, ConfirmAcceptGroupRequestOfferResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IBackgroundJobDispatcher _jobDispatcher;
        private readonly ILogger<ConfirmAcceptGroupRequestOfferCommandHandler> _logger;

        public ConfirmAcceptGroupRequestOfferCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            IBackgroundJobDispatcher jobDispatcher,
            ILogger<ConfirmAcceptGroupRequestOfferCommandHandler> logger)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _jobDispatcher = jobDispatcher;
            _logger = logger;
        }

        public async Task<ConfirmAcceptGroupRequestOfferResponse> Handle(
            ConfirmAcceptGroupRequestOfferCommand request,
            CancellationToken cancellationToken)
        {
            // 1️⃣ Fetch offer with group request and participants
            var offer = await _context.GroupRequestOffers
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .Include(o => o.Responses)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer == null)
            {
                return new ConfirmAcceptGroupRequestOfferResponse
                {
                    Success = false,
                    Error = "Offer not found",
                    ErrorCode = "NOT_FOUND",
                    StatusCode = 404
                };
            }

            // 2️⃣ Validate offer is Open
            if (offer.Status != GroupRequestOfferStatus.Open)
            {
                return new ConfirmAcceptGroupRequestOfferResponse
                {
                    Success = false,
                    Error = $"Offer is {offer.Status}",
                    ErrorCode = "INVALID_OFFER_STATUS",
                    StatusCode = 409
                };
            }

            // 3️⃣ Validate group request is active
            if (offer.GroupRequest.Status != GroupRequestStatus.Active)
            {
                return new ConfirmAcceptGroupRequestOfferResponse
                {
                    Success = false,
                    Error = "Group request is not active",
                    ErrorCode = "INVALID_GROUP_REQUEST_STATUS",
                    StatusCode = 409
                };
            }

            // 4️⃣ Find buyer participant
            var participant = offer.GroupRequest.Participants
                .FirstOrDefault(p => p.BuyerId == request.BuyerId && p.Status == GroupRequestParticipantStatus.Active);

            if (participant == null)
            {
                return new ConfirmAcceptGroupRequestOfferResponse
                {
                    Success = false,
                    Error = "You are not an active participant in this group request",
                    ErrorCode = "NOT_PARTICIPANT",
                    StatusCode = 403
                };
            }

            // 5️⃣ Check if already accepted
            var existingResponse = offer.Responses
                .FirstOrDefault(r => r.BuyerId == request.BuyerId);

            if (existingResponse != null && existingResponse.Response == BuyerOfferResponseType.Accepted)
            {
                return new ConfirmAcceptGroupRequestOfferResponse
                {
                    Success = false,
                    Error = "You have already accepted this offer",
                    ErrorCode = "ALREADY_ACCEPTED",
                    StatusCode = 400
                };
            }

            // 6️⃣ Re-validate capacity (Prevent overselling)
            int remaining = offer.QuantityAvailable - offer.AcceptedQuantity;
            if (request.AcceptedQuantity <= 0)
            {
                return new ConfirmAcceptGroupRequestOfferResponse
                {
                    Success = false,
                    Error = "Accepted quantity must be at least 1.",
                    ErrorCode = "INVALID_QUANTITY",
                    StatusCode = 400
                };
            }

            if (request.AcceptedQuantity > remaining)
            {
                // Rollback Stripe payment hold to prevent charging buyer when save fails
                await _stripePaymentService.CancelPaymentAsync(request.PaymentIntentId, cancellationToken);
                return new ConfirmAcceptGroupRequestOfferResponse
                {
                    Success = false,
                    Error = $"Only {remaining} slot(s) remaining. You cannot accept more than that.",
                    ErrorCode = "INSUFFICIENT_SLOTS",
                    StatusCode = 409
                };
            }

            // 7️⃣ Verify Stripe PaymentIntent status
            var paymentResult = await _stripePaymentService.GetPaymentIntentAsync(request.PaymentIntentId, cancellationToken);
            if (!paymentResult.Success)
            {
                return new ConfirmAcceptGroupRequestOfferResponse
                {
                    Success = false,
                    Error = $"Could not verify payment: {paymentResult.Error}",
                    ErrorCode = "PAYMENT_VERIFICATION_FAILED",
                    StatusCode = 400
                };
            }

            if (paymentResult.Status != "requires_capture" && paymentResult.Status != "succeeded")
            {
                return new ConfirmAcceptGroupRequestOfferResponse
                {
                    Success = false,
                    Error = $"Stripe payment intent status is '{paymentResult.Status}', but 'requires_capture' or 'succeeded' is required.",
                    ErrorCode = "PAYMENT_NOT_AUTHORIZED",
                    StatusCode = 400
                };
            }

            // 8️⃣ Verify Payment amount matches expected amount
            decimal expectedAmount = request.AcceptedQuantity * offer.CurrentUnitPrice;
            long expectedAmountInCents = (long)Math.Round(expectedAmount * 100, MidpointRounding.AwayFromZero);

            if (paymentResult.Amount != expectedAmountInCents)
            {
                // Rollback Stripe payment hold
                await _stripePaymentService.CancelPaymentAsync(request.PaymentIntentId, cancellationToken);
                return new ConfirmAcceptGroupRequestOfferResponse
                {
                    Success = false,
                    Error = "Payment amount does not match the requested quantity.",
                    ErrorCode = "PAYMENT_AMOUNT_MISMATCH",
                    StatusCode = 400
                };
            }

            // 9️⃣ Create or update buyer response
            var currentAcceptedQuantity = offer.AcceptedQuantity;
            try
            {
                if (existingResponse == null)
                {
                    existingResponse = new BuyerOfferResponse
                    {
                        OfferId = request.OfferId,
                        BuyerId = request.BuyerId,
                        Response = BuyerOfferResponseType.Accepted,
                        AcceptedQuantity = request.AcceptedQuantity,
                        StripePaymentIntentId = request.PaymentIntentId,
                        RespondedAt = DateTime.UtcNow
                    };
                    _context.BuyerOfferResponses.Add(existingResponse);
                    offer.Responses.Add(existingResponse);
                }
                else
                {
                    existingResponse.Response = BuyerOfferResponseType.Accepted;
                    existingResponse.AcceptedQuantity = request.AcceptedQuantity;
                    existingResponse.StripePaymentIntentId = request.PaymentIntentId;
                    existingResponse.RespondedAt = DateTime.UtcNow;
                }

                // Update the persisted AcceptedQuantity counter on the offer
                offer.AcceptedQuantity = currentAcceptedQuantity + request.AcceptedQuantity;

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save buyer offer response for buyer {BuyerId}. Cancelling payment hold {PaymentIntentId}",
                    request.BuyerId, request.PaymentIntentId);

                // Revert/Cancel Stripe Payment Hold if DB save failed
                await _stripePaymentService.CancelPaymentAsync(request.PaymentIntentId, cancellationToken);

                return new ConfirmAcceptGroupRequestOfferResponse
                {
                    Success = false,
                    Error = "An error occurred while saving your response. Payment hold was released.",
                    ErrorCode = "SAVE_FAILED",
                    StatusCode = 500
                };
            }

            // 🔟 Check if offer is complete
            bool isComplete = offer.AcceptedQuantity >= offer.QuantityAvailable;

            if (isComplete)
            {
                // Offload to background job
                _jobDispatcher.Enqueue<IGroupRequestOfferFillJob>(j => j.ExecuteAsync(request.OfferId));
            }

            return new ConfirmAcceptGroupRequestOfferResponse
            {
                Success = true
            };
        }
    }
}
