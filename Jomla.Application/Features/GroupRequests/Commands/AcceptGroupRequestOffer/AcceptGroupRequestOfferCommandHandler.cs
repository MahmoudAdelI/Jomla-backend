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

            return new AcceptGroupRequestOfferResponse
            {
                Success = true,
                OfferId = request.OfferId,
                GroupRequestId = offer.GroupRequestId,
                PaymentIntentId = paymentResult.PaymentIntentId,
                ClientSecret = paymentResult.ClientSecret,
                AcceptedQuantity = request.AcceptedQuantity,
                TotalAmount = totalAmount,
                Message = "Payment intent created successfully. Please confirm payment."
            };

        }
    }
}
