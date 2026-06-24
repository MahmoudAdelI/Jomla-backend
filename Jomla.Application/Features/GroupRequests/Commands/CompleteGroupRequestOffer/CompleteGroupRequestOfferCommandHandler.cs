using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.CompleteGroupRequestOffer
{
    public class CompleteGroupRequestOfferCommandHandler : IRequestHandler<CompleteGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IMediator _mediator;
        private readonly ILogger<CompleteGroupRequestOfferCommandHandler> _logger;

        public CompleteGroupRequestOfferCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            IMediator mediator,
            ILogger<CompleteGroupRequestOfferCommandHandler> logger)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(CompleteGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _context.GroupRequestOffers
                .Include(o => o.Responses)
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer == null || offer.Status != GroupRequestOfferStatus.Open)
                return;

            offer.Status = GroupRequestOfferStatus.Accepted;
            await _context.SaveChangesAsync(cancellationToken);

            var activeResponses = offer.Responses
                .Where(r => r.Response == BuyerOfferResponseType.Accepted)
                .ToList();

            bool hasFailure = false;
            var successfulBuyerIds = new List<Guid>();

            foreach (var response in activeResponses)
            {
                try
                {
                    var participant = offer.GroupRequest.Participants
                        .FirstOrDefault(p => p.BuyerId == response.BuyerId && p.Status == GroupRequestParticipantStatus.Active);

                    if (participant == null)
                    {
                        _logger.LogWarning("Buyer {BuyerId} accepted offer {OfferId} but is not an active participant of GroupRequest {GroupRequestId}",
                            response.BuyerId, offer.Id, offer.GroupRequestId);
                        continue;
                    }

                    var existingOrder = await _context.Orders
                        .FirstOrDefaultAsync(o => o.OfferId == offer.Id && o.BuyerId == response.BuyerId, cancellationToken);

                    if (existingOrder != null && existingOrder.Status == OrderStatus.Paid)
                        continue;

                    if (string.IsNullOrEmpty(response.StripePaymentIntentId))
                    {
                        _logger.LogWarning("StripePaymentIntentId is missing for buyer {BuyerId} response on offer {OfferId}", response.BuyerId, offer.Id);
                        continue;
                    }

                    var captureResult = await _stripePaymentService.CapturePaymentAsync(response.StripePaymentIntentId, cancellationToken);

                    if (existingOrder != null)
                    {
                        existingOrder.Status = captureResult.Success ? OrderStatus.Paid : OrderStatus.Failed;
                        existingOrder.PaidAt = captureResult.Success ? DateTime.UtcNow : null;
                    }
                    else
                    {
                        var order = new Order
                        {
                            BuyerId = response.BuyerId,
                            BatchId = null,
                            OfferId = offer.Id,
                            Quantity = participant.Quantity,
                            TotalAmount = participant.Quantity * offer.UnitPrice,
                            Status = captureResult.Success ? OrderStatus.Paid : OrderStatus.Failed,
                            PaidAt = captureResult.Success ? DateTime.UtcNow : null,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Orders.Add(order);
                    }

                    await _context.SaveChangesAsync(cancellationToken);

                    if (captureResult.Success)
                    {
                        successfulBuyerIds.Add(response.BuyerId);
                    }
                    else
                    {
                        hasFailure = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Capture failed for buyer {BuyerId} in group request offer {OfferId}", response.BuyerId, offer.Id);
                    hasFailure = true;
                }
            }

            if (hasFailure)
            {
                throw new Exception("One or more captures failed for GroupRequestOffer. Hangfire will retry.");
            }

            // Create notification for successful captures
            foreach (var buyerId in successfulBuyerIds)
            {
                var notification = new Notification
                {
                    UserId = buyerId,
                    Type = NotificationType.GroupRequestOfferFilled,
                    Title = "Group Request Offer Filled",
                    Body = "Your accepted group request offer has been filled and payment was captured.",
                    EntityId = offer.Id,
                    EntityType = nameof(GroupRequestOffer),
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync(cancellationToken);

                await _mediator.Publish(new NotificationCreatedEvent(buyerId, notification.Id), cancellationToken);
            }
        }
    }
}
