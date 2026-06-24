using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.CancelGroupRequestOffer
{
    public class CancelGroupRequestOfferCommandHandler : IRequestHandler<CancelGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly ILogger<CancelGroupRequestOfferCommandHandler> _logger;

        public CancelGroupRequestOfferCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            ILogger<CancelGroupRequestOfferCommandHandler> logger)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _logger = logger;
        }

        public async Task Handle(CancelGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _context.GroupRequestOffers
                .Include(o => o.Responses)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer == null)
                return;

            var activeResponses = offer.Responses
                .Where(r => r.Response == BuyerOfferResponseType.Accepted)
                .ToList();

            foreach (var response in activeResponses)
            {
                try
                {
                    if (!string.IsNullOrEmpty(response.StripePaymentIntentId))
                    {
                        var cancelResult = await _stripePaymentService.CancelPaymentAsync(
                            response.StripePaymentIntentId,
                            cancellationToken: cancellationToken);

                        if (!cancelResult.Success)
                        {
                            _logger.LogWarning("Failed to cancel payment hold {PaymentIntentId} for buyer {BuyerId} on offer {OfferId}: {Error}",
                                response.StripePaymentIntentId, response.BuyerId, offer.Id, cancelResult.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cancelling Stripe payment hold {PaymentIntentId} for buyer {BuyerId} on offer {OfferId}",
                        response.StripePaymentIntentId, response.BuyerId, offer.Id);
                }

                response.Response = BuyerOfferResponseType.Cancelled;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
