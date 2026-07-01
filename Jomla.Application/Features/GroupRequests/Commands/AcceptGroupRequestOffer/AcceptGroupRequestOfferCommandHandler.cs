using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using Jomla.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Jomla.Application.Common.Exceptions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.AcceptGroupRequestOffer
{
    public class AcceptGroupRequestOfferCommandHandler(
        IAppDbContext context,
        IStripePaymentService stripePaymentService,
        ILogger<AcceptGroupRequestOfferCommandHandler> logger) : IRequestHandler<AcceptGroupRequestOfferCommand, AcceptGroupRequestOfferResponse>
    {
        private readonly IAppDbContext _context = context;
        private readonly IStripePaymentService _stripePaymentService = stripePaymentService;
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
                throw new NotFoundException(nameof(GroupRequestOffer), request.OfferId);

            // Step 2: Validate offer is Open
            if (offer.Status != GroupRequestOfferStatus.Open)
                throw new ConflictException($"Offer status is {offer.Status}.");

            // Step 3: Validate group request is active
            if (offer.GroupRequest.Status != GroupRequestStatus.Active)
                throw new ConflictException("Group request is not active.");

            // Step 4: Find buyer participant
            var participant = offer.GroupRequest.Participants
                .FirstOrDefault(p => p.BuyerId == request.BuyerId && p.Status == GroupRequestParticipantStatus.Active);

            if (participant == null)
                throw new ForbiddenException("You are not an active participant in this group request.");

            // Step 5: Check if already accepted
            var existingResponse = offer.Responses
                .FirstOrDefault(r => r.BuyerId == request.BuyerId);

            if (existingResponse != null && existingResponse.Response == BuyerOfferResponseType.Accepted)
                throw new ConflictException("You have already accepted this offer.");

            // Validate AcceptedQuantity bounds
            int remaining = offer.QuantityAvailable - offer.AcceptedQuantity;

            if (request.AcceptedQuantity <= 0)
                throw new BadRequestException("Accepted quantity must be at least 1.");

            if (request.AcceptedQuantity > remaining)
                throw new ConflictException($"Only {remaining} slot(s) remaining. You cannot accept more than that.");

            // Calculate payment amount using the buyer-chosen accepted quantity
            decimal totalAmount = request.AcceptedQuantity * offer.CurrentUnitPrice;

            // Step 7: Create Stripe payment hold
            var paymentResult = await _stripePaymentService.CreatePaymentHoldAsync(
                request.BuyerId.ToString(),
                request.BuyerEmail,
                totalAmount,
                request.OfferId,
                cancellationToken: cancellationToken);

            if (!paymentResult.Success)
            {
                _logger.LogWarning("Payment hold failed for buyer {BuyerId} on offer {OfferId}: {Error}",
                    request.BuyerId, request.OfferId, paymentResult.Error);
                throw new BadRequestException($"Payment hold failed: {paymentResult.Error}");
            }

            return new AcceptGroupRequestOfferResponse
            {
                OfferId = request.OfferId,
                GroupRequestId = offer.GroupRequestId,
                PaymentIntentId = paymentResult.PaymentIntentId,
                ClientSecret = paymentResult.ClientSecret,
                AcceptedQuantity = request.AcceptedQuantity,
                TotalAmount = totalAmount
            };
        }
    }
}
