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

using Jomla.Application.Features.GroupRequests.Queries;
using Jomla.Application.Jobs.Closing;
using Jomla.Application.Jobs.JobDispatcher;

namespace Jomla.Application.Features.GroupRequests.Commands.CompleteGroupRequestOffer
{
    public class CompleteGroupRequestOfferCommandHandler : IRequestHandler<CompleteGroupRequestOfferCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly IMediator _mediator;
        private readonly IRealtimeService _realtimeService;
        private readonly IBackgroundJobDispatcher _jobDispatcher;
        private readonly ILogger<CompleteGroupRequestOfferCommandHandler> _logger;

        public CompleteGroupRequestOfferCommandHandler(
            IAppDbContext context,
            IStripePaymentService stripePaymentService,
            IMediator mediator,
            IRealtimeService realtimeService,
            IBackgroundJobDispatcher jobDispatcher,
            ILogger<CompleteGroupRequestOfferCommandHandler> logger)
        {
            _context = context;
            _stripePaymentService = stripePaymentService;
            _mediator = mediator;
            _realtimeService = realtimeService;
            _jobDispatcher = jobDispatcher;
            _logger = logger;
        }

        public async Task Handle(CompleteGroupRequestOfferCommand request, CancellationToken cancellationToken)
        {
            GroupRequestOffer? offer = null;

            //  PHASE 1: DB Transaction (Fast — No Stripe calls)
            // Lock the offer and create Pending orders before touching Stripe

            using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    // Fetch the offer with all accepted responses and active participants
                    offer = await _context.GroupRequestOffers
                        .Include(o => o.Responses)
                        .Include(o => o.GroupRequest)
                            .ThenInclude(gr => gr.Participants)
                        .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

                    // If offer is missing or already processed, exit safely (idempotency guard)
                    if (offer == null || offer.Status != GroupRequestOfferStatus.Open)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }

                    // Lock the offer immediately to prevent any concurrent processing
                    offer.Status = GroupRequestOfferStatus.Accepted;

                    var activeResponses = offer.Responses
                        .Where(r => r.Response == BuyerOfferResponseType.Accepted)
                        .ToList();

                    foreach (var response in activeResponses)
                    {
                        // Skip if buyer is no longer an active participant
                        var participant = offer.GroupRequest.Participants
                            .FirstOrDefault(p => p.BuyerId == response.BuyerId
                                              && p.Status == GroupRequestParticipantStatus.Active);

                        if (participant == null)
                        {
                            _logger.LogWarning(
                                "Buyer {BuyerId} accepted offer {OfferId} but is not an active participant",
                                response.BuyerId, offer.Id);
                            continue;
                        }

                        // Skip if payment intent is missing — cannot capture without it
                        if (string.IsNullOrEmpty(response.StripePaymentIntentId))
                        {
                            _logger.LogWarning(
                                "StripePaymentIntentId is missing for buyer {BuyerId}",
                                response.BuyerId);
                            continue;
                        }

                        // Check if an order already exists for this buyer on this offer (Retry safety)
                        var existingOrder = await _context.Orders
                            .FirstOrDefaultAsync(o => o.OfferId == offer.Id
                                                   && o.BuyerId == response.BuyerId,
                                                   cancellationToken);

                        // Already paid — skip to avoid double charging
                        if (existingOrder != null && existingOrder.Status == OrderStatus.Paid)
                            continue;

                        // No order yet — create one with Pending status (Stripe hasn't run yet)
                        if (existingOrder == null)
                        {
                            _context.Orders.Add(new Order
                            {
                                BuyerId = response.BuyerId,
                                BatchId = null,
                                OfferId = offer.Id,
                                Quantity = participant.Quantity,
                                TotalAmount = participant.Quantity * offer.CurrentUnitPrice,
                                Status = OrderStatus.Pending,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        // Order exists but failed previously — reset to Pending for retry
                        else if (existingOrder.Status == OrderStatus.Failed)
                        {
                            existingOrder.Status = OrderStatus.Pending;
                        }
                    }

                    // Persist the offer status change and all Pending orders to DB
                    await _context.SaveChangesAsync(cancellationToken);

                    // Commit the transaction — DB is now consistent before any Stripe calls
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex,
                        "Phase 1 failed: Could not prepare orders for offer {OfferId}",
                        request.OfferId);
                    throw;
                }
            }

            //  PHASE 2: Stripe Captures (Outside Transaction — Safe to Retry)
            // Each order is saved individually so retries can resume from where they stopped

            // Fetch all Pending orders for this offer to process
            var pendingOrders = await _context.Orders
                .Where(o => o.OfferId == request.OfferId && o.Status == OrderStatus.Pending)
                .ToListAsync(cancellationToken);

            // Fetch accepted responses to get the PaymentIntentId for each buyer
            var responses = await _context.BuyerOfferResponses
                .Where(r => r.OfferId == request.OfferId
                         && r.Response == BuyerOfferResponseType.Accepted)
                .ToListAsync(cancellationToken);

            bool anyCaptureFailed = false;
            var successfulBuyerIds = new List<Guid>();

            foreach (var order in pendingOrders)
            {
                var response = responses.FirstOrDefault(r => r.BuyerId == order.BuyerId);

                // Skip if no payment intent found for this buyer
                if (response == null || string.IsNullOrEmpty(response.StripePaymentIntentId))
                {
                    _logger.LogWarning(
                        "No payment intent found for buyer {BuyerId} on offer {OfferId}",
                        order.BuyerId, request.OfferId);
                    continue;
                }

                // Capture the actual payment from Stripe using idempotency key to prevent double charging on retry
                var captureResult = await _stripePaymentService.CapturePaymentAsync(
                    response.StripePaymentIntentId,
                    idempotencyKey: $"capture-{request.OfferId}-{order.BuyerId}",
                    cancellationToken);

                if (captureResult.Success)
                {
                    order.Status = OrderStatus.Paid;
                    order.PaidAt = DateTime.UtcNow;
                    successfulBuyerIds.Add(order.BuyerId);

                    var participant = offer!.GroupRequest.Participants
                        .FirstOrDefault(p => p.BuyerId == order.BuyerId);
                    if (participant != null)
                    {
                        participant.Status = GroupRequestParticipantStatus.Fulfilled;
                        offer.GroupRequest.CurrentQuantity -= participant.Quantity;
                    }
                }
                else
                {
                    order.Status = OrderStatus.Failed;
                    anyCaptureFailed = true;
                    _logger.LogWarning(
                        "Payment capture failed for buyer {BuyerId}, offer {OfferId}: {Error}",
                        order.BuyerId, request.OfferId, captureResult.Error);
                }

                // Save each order result immediately so retries can skip already-processed ones
                await _context.SaveChangesAsync(cancellationToken);
            }


            // PHASE 2B: Partial failure — refund all successful captures
            // If any capture failed, refund everyone who was already charged

            if (anyCaptureFailed)
            {
                _logger.LogWarning(
                    "Partial capture failure detected for offer {OfferId}. Refunding {Count} successful captures.",
                    request.OfferId, successfulBuyerIds.Count);

                foreach (var buyerId in successfulBuyerIds)
                {
                    var successResponse = responses.First(r => r.BuyerId == buyerId);
                    try
                    {
                        // Refund the buyer who was already charged
                        await _stripePaymentService.RefundPaymentAsync(
                            successResponse.StripePaymentIntentId,
                            cancellationToken);

                        // Update order status to Refunded in DB
                        var refundedOrder = await _context.Orders
                            .FirstOrDefaultAsync(o => o.OfferId == request.OfferId
                                                   && o.BuyerId == buyerId,
                                                   cancellationToken);
                        if (refundedOrder != null)
                        {
                            refundedOrder.Status = OrderStatus.Refunded;
                            refundedOrder.PaidAt = null;

                            var participant = offer!.GroupRequest.Participants
                                .FirstOrDefault(p => p.BuyerId == buyerId);
                            if (participant != null)
                            {
                                participant.Status = GroupRequestParticipantStatus.Active;
                                offer.GroupRequest.CurrentQuantity += participant.Quantity;
                            }

                            await _context.SaveChangesAsync(cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Refund itself failed — log as Critical for manual intervention
                        _logger.LogCritical(ex,
                            "CRITICAL: Failed to refund PaymentIntent {PaymentIntentId} for buyer {BuyerId}. Manual intervention required!",
                            successResponse.StripePaymentIntentId, buyerId);
                    }
                }

                throw new Exception($"One or more captures failed for offer {request.OfferId}. Successful captures have been refunded.");
            }


            // PHASE 3: Notifications (After everything is done successfully)
            // Save all notifications in a single bulk insert, then fire real-time events

            if (offer!.GroupRequest.CurrentQuantity <= 0)
            {
                offer.GroupRequest.CurrentQuantity = 0;
                
                if (offer.GroupRequest.Status != GroupRequestStatus.Inactive && offer.GroupRequest.Status != GroupRequestStatus.Closed)
                {
                    offer.GroupRequest.Status = GroupRequestStatus.Inactive;
                    offer.GroupRequest.InactiveSince = DateTime.UtcNow;

                    _jobDispatcher.Schedule<IGroupRequestAutoCloseJob>(
                        j => j.ExecuteAsync(offer.GroupRequestId),
                        DateTimeOffset.UtcNow.AddHours(24));
                }
            }

            // Build all notifications at once — single DB round trip
            var notifications = successfulBuyerIds.Select(buyerId => new Notification
            {
                UserId = buyerId,
                Type = NotificationType.GroupRequestOfferFilled,
                Title = "Group Request Offer Filled",
                Body = "Your accepted group request offer has been filled and payment was captured.",
                EntityId = offer!.GroupRequestId,
                EntityType = nameof(GroupRequest),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            if (notifications.Any())
            {
                // Bulk insert — single DB round trip for all notifications
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync(cancellationToken);

                // Fire real-time event for each buyer — failure here does not affect financials
                foreach (var notification in notifications)
                {
                    try
                    {
                        await _mediator.Publish(
                            new NotificationCreatedEvent(notification.UserId, notification.Id),
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Real-time notification failed for buyer {BuyerId} — financials unaffected",
                            notification.UserId);
                    }
                }
            }

            try
            {
                var detail = await _mediator.Send(new GetGroupRequestDetailQuery(offer!.GroupRequestId), cancellationToken);
                if (detail != null)
                {
                    await _realtimeService.SendGroupRequestUpdatedAsync(offer!.GroupRequestId, detail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send GroupRequestUpdated SignalR broadcast for GroupRequest {GroupRequestId}", offer!.GroupRequestId);
            }

            _logger.LogInformation(
                "Offer {OfferId} completed successfully. {Count} buyers charged.",
                request.OfferId, successfulBuyerIds.Count);
        }
    
    }
}