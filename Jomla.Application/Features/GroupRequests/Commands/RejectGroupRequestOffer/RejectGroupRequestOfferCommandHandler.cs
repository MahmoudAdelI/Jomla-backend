using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Commands.NegotiateGroupRequestOffer;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.RejectGroupRequestOffer
{
    public class RejectGroupRequestOfferCommandHandler : IRequestHandler<RejectGroupRequestOfferCommand, RejectGroupRequestOfferResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IMediator _mediator;
        private readonly ILogger<RejectGroupRequestOfferCommandHandler> _logger;
        private readonly IRealtimeService _realtimeService;

        public RejectGroupRequestOfferCommandHandler(
            IAppDbContext context,
            IMediator mediator,
            ILogger<RejectGroupRequestOfferCommandHandler> logger,
            IRealtimeService realtimeService)
        {
            _context = context;
            _mediator = mediator;
            _logger = logger;
            _realtimeService = realtimeService;
        }

        public async Task<RejectGroupRequestOfferResponse> Handle(RejectGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _context.GroupRequestOffers
                .Include(o => o.Responses)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Category)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer == null)
            {
                return new RejectGroupRequestOfferResponse { Success = false, Error = "Offer not found." };
            }

            if (offer.Status != GroupRequestOfferStatus.Open)
            {
                return new RejectGroupRequestOfferResponse { Success = false, Error = $"Offer is already {offer.Status}." };
            }

            var participant = offer.GroupRequest.Participants
                .FirstOrDefault(p => p.BuyerId == request.BuyerId && p.Status == GroupRequestParticipantStatus.Active);

            if (participant == null)
            {
                return new RejectGroupRequestOfferResponse { Success = false, Error = "You are not an active participant in this group request." };
            }

            var existingResponse = offer.Responses.FirstOrDefault(r => r.BuyerId == request.BuyerId);

            if (existingResponse != null && existingResponse.Response == BuyerOfferResponseType.Accepted)
            {
                return new RejectGroupRequestOfferResponse { Success = false, Error = "You must cancel your acceptance before you can reject the offer." };
            }

            if (existingResponse != null && existingResponse.Response == BuyerOfferResponseType.Rejected)
            {
                // Already rejected
                return new RejectGroupRequestOfferResponse { Success = true };
            }

            if (existingResponse == null)
            {
                existingResponse = new BuyerOfferResponse
                {
                    OfferId = request.OfferId,
                    BuyerId = request.BuyerId,
                    Response = BuyerOfferResponseType.Rejected,
                    StripePaymentIntentId = null,
                    RespondedAt = DateTime.UtcNow
                };
                _context.BuyerOfferResponses.Add(existingResponse);
                offer.Responses.Add(existingResponse);
            }
            else
            {
                existingResponse.Response = BuyerOfferResponseType.Rejected;
                existingResponse.StripePaymentIntentId = null;
                existingResponse.RespondedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Buyer {BuyerId} rejected offer {OfferId}.", request.BuyerId, request.OfferId);

            var rejectionsCount = offer.Responses.Count(r => r.Response == BuyerOfferResponseType.Rejected);
            if (rejectionsCount >= 3)
            {
                _logger.LogInformation("Rejection threshold met (3+) for offer {OfferId}. Triggering AI Negotiation Agent.", offer.Id);
                await _mediator.Send(new NegotiateGroupRequestOfferCommand(offer.Id, offer.GroupRequest.Category.Name), cancellationToken);
            }

            try
            {
                var detail = await _mediator.Send(new Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestDetailQuery(offer.GroupRequestId), cancellationToken);
                if (detail != null)
                {
                    await _realtimeService.SendGroupRequestUpdatedAsync(offer.GroupRequestId, detail);
                }
            }
            catch
            {
                // Non-blocking SignalR fallback
            }

            return new RejectGroupRequestOfferResponse { Success = true };
        }
    }
}
