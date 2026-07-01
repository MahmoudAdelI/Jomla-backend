using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using Jomla.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Jomla.Application.Jobs.Fulfillment;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Application.Common.Exceptions;
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
    ) : IRequest<bool>;

    public class ConfirmAcceptGroupRequestOfferCommandHandler : IRequestHandler<ConfirmAcceptGroupRequestOfferCommand, bool>
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

        public async Task<bool> Handle(
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
                throw new NotFoundException(nameof(GroupRequestOffer), request.OfferId);

            // 2️⃣ Validate offer is Open
            if (offer.Status != GroupRequestOfferStatus.Open)
                throw new ConflictException($"Offer status is {offer.Status}.");

            // 3️⃣ Validate group request is active
            if (offer.GroupRequest.Status != GroupRequestStatus.Active)
                throw new ConflictException("Group request is not active.");

            // 4️⃣ Find buyer participant
            var participant = offer.GroupRequest.Participants
                .FirstOrDefault(p => p.BuyerId == request.BuyerId && p.Status == GroupRequestParticipantStatus.Active);

            if (participant == null)
                throw new ForbiddenException("You are not an active participant in this group request.");

            // 5️⃣ Check if already accepted
            var existingResponse = offer.Responses
                .FirstOrDefault(r => r.BuyerId == request.BuyerId);

            if (existingResponse != null && existingResponse.Response == BuyerOfferResponseType.Accepted)
                throw new ConflictException("You have already accepted this offer.");

            // 6️⃣ Re-validate capacity (Prevent overselling)
            int remaining = offer.QuantityAvailable - offer.AcceptedQuantity;
            if (request.AcceptedQuantity <= 0)
                throw new BadRequestException("Accepted quantity must be at least 1.");

            if (request.AcceptedQuantity > remaining)
            {
                // Rollback Stripe payment hold to prevent charging buyer
                await _stripePaymentService.CancelPaymentAsync(request.PaymentIntentId, cancellationToken);
                throw new ConflictException($"Only {remaining} slot(s) remaining. You cannot accept more than that.");
            }

            // 7️⃣ Verify Stripe PaymentIntent status
            var paymentResult = await _stripePaymentService.GetPaymentIntentAsync(request.PaymentIntentId, cancellationToken);
            if (!paymentResult.Success)
                throw new BadRequestException($"Could not verify payment: {paymentResult.Error}");

            if (paymentResult.Status != "requires_capture" && paymentResult.Status != "succeeded")
                throw new BadRequestException($"Stripe payment intent status is '{paymentResult.Status}', but 'requires_capture' or 'succeeded' is required.");

            // 8️⃣ Verify Payment amount matches expected amount
            decimal expectedAmount = request.AcceptedQuantity * offer.CurrentUnitPrice;
            long expectedAmountInCents = (long)Math.Round(expectedAmount * 100, MidpointRounding.AwayFromZero);

            if (paymentResult.Amount != expectedAmountInCents)
            {
                // Rollback Stripe payment hold
                await _stripePaymentService.CancelPaymentAsync(request.PaymentIntentId, cancellationToken);
                throw new BadRequestException("Payment amount does not match the requested quantity.");
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

                throw new ConflictException("An error occurred while saving your response. Payment hold was released.");
            }

            // 🔟 Check if offer is complete
            bool isComplete = offer.AcceptedQuantity >= offer.QuantityAvailable;

            if (isComplete)
            {
                // Offload to background job
                _jobDispatcher.Enqueue<IGroupRequestOfferFillJob>(j => j.ExecuteAsync(request.OfferId));
            }

            return true;
        }
    }
}
