using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using Jomla.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Jomla.Application.Features.Events;

namespace Jomla.Application.Features.GroupRequests.Commands.AcceptGroupRequestOffer
{
    public class AcceptGroupRequestOfferCommandHandler : IRequestHandler<AcceptGroupRequestOfferCommand, AcceptGroupRequestOfferResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IMediator _mediator;
        private readonly ILogger<AcceptGroupRequestOfferCommandHandler> _logger;

        public AcceptGroupRequestOfferCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            IMediator mediator,
            ILogger<AcceptGroupRequestOfferCommandHandler> logger)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _mediator = mediator;
            _logger = logger;
        }

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
                return new AcceptGroupRequestOfferResponse { Success = false, Error = "You are not a participant" };

            // Step 5: Check if already accepted
            var existingResponse = offer.Responses
                .FirstOrDefault(r => r.BuyerId == request.BuyerId);

            if (existingResponse != null && existingResponse.Response == BuyerOfferResponseType.Accepted)
                return new AcceptGroupRequestOfferResponse { Success = false, Error = "Already accepted" };

            // Step 6: Calculate payment amount
            decimal totalAmount = participant.Quantity * offer.CurrentUnitPrice;

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

            // Step 8: Create or update buyer response
            if (existingResponse == null)
            {
                existingResponse = new BuyerOfferResponse
                {
                    OfferId = request.OfferId,
                    BuyerId = request.BuyerId,
                    Response = BuyerOfferResponseType.Accepted,
                    StripePaymentIntentId = paymentResult.PaymentIntentId,
                    RespondedAt = DateTime.UtcNow
                };
                _context.BuyerOfferResponses.Add(existingResponse);
            }
            else
            {
                existingResponse.Response = BuyerOfferResponseType.Accepted;
                existingResponse.StripePaymentIntentId = paymentResult.PaymentIntentId;
            }

            // Step 9: Save
            await _context.SaveChangesAsync(cancellationToken);

            // Step 10: Calculate total accepted quantity
            var acceptedBuyerIds = offer.Responses
                .Where(r => r.Response == BuyerOfferResponseType.Accepted)
                .Select(r => r.BuyerId)
                .ToHashSet();

            var acceptedQuantity = offer.GroupRequest.Participants
                .Where(p =>
                    acceptedBuyerIds.Contains(p.BuyerId) &&
                    p.Status == GroupRequestParticipantStatus.Active)
                .Sum(p => p.Quantity);

            // Step 11: Check if offer is complete
            bool isComplete = acceptedQuantity >= offer.QuantityAvailable;

            if (isComplete)
            {
                // Publish event to trigger completion
                await _mediator.Publish(
                    new OfferAcceptedCompleteEvent(request.OfferId),
                    cancellationToken);
            }

            return new AcceptGroupRequestOfferResponse
            {
                Success = true,
                OfferId = request.OfferId,
                GroupRequestId = offer.GroupRequestId,
                PaymentIntentId = paymentResult.PaymentIntentId,
                AcceptedQuantity = acceptedQuantity,
                TotalAmount = totalAmount,
                Message = isComplete
                    ? "Offer accepted! All slots filled, processing..."
                    : $"Offer accepted! {acceptedQuantity}/{offer.QuantityAvailable} slots filled"
            };
        }
    }

   
}

