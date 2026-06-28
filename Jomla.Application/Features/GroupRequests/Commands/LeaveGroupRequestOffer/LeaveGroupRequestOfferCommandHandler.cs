using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.LeaveGroupRequestOffer
{
    public class LeaveGroupRequestOfferCommandHandler : IRequestHandler<LeaveGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly ILogger<LeaveGroupRequestOfferCommandHandler> _logger;

        public LeaveGroupRequestOfferCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            ILogger<LeaveGroupRequestOfferCommandHandler> logger)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _logger = logger;
        }
            public async Task Handle(LeaveGroupRequestOfferCommand request, CancellationToken cancellationToken)
            {
                // 1️⃣ Fetch the offer with its responses
                var offer = await _context.GroupRequestOffers
                    .Include(o => o.Responses)
                    .Include(o => o.GroupRequest)
                        .ThenInclude(gr => gr.Participants)
                    .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

                if (offer == null || offer.Status != GroupRequestOfferStatus.Open)
                {
                    throw new InvalidOperationException("Cannot leave the offer because the group is no longer open.");
                }

                // 2️⃣ Find the buyer's response row
                var buyerResponse = offer.Responses
                    .FirstOrDefault(r => r.BuyerId == request.BuyerId);

                if (buyerResponse == null)
                {
                    return; // Buyer never joined
                }

                // ⭐ [Idempotency Guard 1]: If already cancelled in DB, stop immediately.
                // This prevents duplicate accounting if Hangfire retries a succeeding job.
                if (buyerResponse.Response == BuyerOfferResponseType.Cancelled)
                {
                    _logger.LogInformation("Buyer {BuyerId} has already left offer {OfferId}. Skipping.", request.BuyerId, request.OfferId);
                    return;
                }

                // 3️⃣ Interact with Stripe (External Service)
                if (!string.IsNullOrEmpty(buyerResponse.StripePaymentIntentId))
                {
                    try
                    {
                        var cancelResult = await _stripePaymentService.CancelPaymentAsync(
                            buyerResponse.StripePaymentIntentId,
                            cancellationToken: cancellationToken);

                        // ⭐ [Idempotency Guard 2]: Handle Stripe's response smartly
                        if (!cancelResult.Success)
                        {
                            // If Stripe says it's already refunded/canceled, we shouldn't fail! 
                            // We treat it as success and move forward to fix our DB.
                            if (cancelResult.Error == "charge_already_refunded" || cancelResult.Error == "payment_intent_canceled")
                            {
                                _logger.LogWarning("Stripe hold was already released for buyer {BuyerId} on offer {OfferId}. Proceeding to sync DB.", request.BuyerId, request.OfferId);
                            }
                            else
                            {
                                // Actual network or API failure -> throw exception so Hangfire retries later
                                throw new InvalidOperationException($"Stripe operation failed: {cancelResult.Error}");
                            }
                        }
                    }
                    catch (Exception ex) when (ex.Message.Contains("already refunded") || ex.Message.Contains("canceled"))
                    {
                        _logger.LogWarning(ex, "Stripe exception caught indicating hold was already released for buyer {BuyerId}. Syncing DB.", request.BuyerId);
                    }
                }

                // 4️⃣ Update state inside our system
                buyerResponse.Response = BuyerOfferResponseType.Cancelled;

                // Recalculate accepted quantity
                var acceptedBuyerIds = offer.Responses
                    .Where(r => r.Response == BuyerOfferResponseType.Accepted)
                    .Select(r => r.BuyerId)
                    .ToHashSet();

                var acceptedQuantity = offer.GroupRequest.Participants
                    .Where(p =>
                        acceptedBuyerIds.Contains(p.BuyerId) &&
                        p.Status == GroupRequestParticipantStatus.Active)
                    .Sum(p => p.Quantity);

                offer.AcceptedQuantity = acceptedQuantity;

                // 5️⃣ Persist changes. If this fails, Hangfire will retry the whole method.
                // Thanks to Guards 1 & 2, retrying will NOT cause double-refunds on Stripe!
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Buyer {BuyerId} successfully left the offer {OfferId} and DB is synchronized.", request.BuyerId, request.OfferId);
            }
        
    }
}

