using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Queries;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Jobs.Expiry;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.ApproveNegotiation
{
    public record ApproveNegotiationCommand(Guid OfferId, Guid SupplierId) : IRequest<bool>;

    public class ApproveNegotiationCommandHandler(
        IAppDbContext db,
        IBackgroundJobDispatcher jobDispatcher,
        IMediator mediator,
        IRealtimeService realtimeService
        ) : IRequestHandler<ApproveNegotiationCommand, bool>
    {
        private readonly IAppDbContext _db = db;
        private readonly IBackgroundJobDispatcher _jobDispatcher = jobDispatcher;
        private readonly IMediator _mediator = mediator;
        private readonly IRealtimeService _realtimeService = realtimeService;

        public async Task<bool> Handle(ApproveNegotiationCommand request, CancellationToken cancellationToken)
        {
            var offer = await _db.GroupRequestOffers
                .Include(o => o.GroupRequest)
                    .ThenInclude(gr => gr.Participants)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer == null)
                throw new NotFoundException(nameof(GroupRequestOffer), request.OfferId);

            if (offer.SupplierId != request.SupplierId)
                throw new ForbiddenException("You are not authorized to approve this negotiation proposal.");

            if (offer.Status != GroupRequestOfferStatus.PendingSupplierApproval)
                throw new ConflictException($"Offer status is {offer.Status}, cannot approve.");

            // 1. Mark status as Open
            offer.Status = GroupRequestOfferStatus.Open;

            // 2. Schedule Hangfire expiry job
            offer.JobId = _jobDispatcher.Schedule<IGroupRequestOfferExpiryJob>(job =>
                job.ExcuteAsync(offer.Id),
                new DateTimeOffset(offer.ExpiresAt, TimeSpan.Zero));

            // 3. Notify all active participants about the new offer
            var participantIds = offer.GroupRequest.Participants
                .Where(p => p.Status == GroupRequestParticipantStatus.Active)
                .Select(p => p.BuyerId);

            var notifications = participantIds.Select(buyerId => new Notification
            {
                UserId = buyerId,
                Type = NotificationType.GroupRequestOfferPlaced,
                Title = "New offer price available",
                Body = $"A new price of {offer.CurrentUnitPrice:C} has been offered for {offer.GroupRequest.Title}.",
                EntityId = offer.GroupRequestId,
                EntityType = nameof(GroupRequest),
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync(cancellationToken);

            // 4. Broadcast SignalR update
            try
            {
                var detail = await _mediator.Send(new GetGroupRequestDetailQuery(offer.GroupRequestId), cancellationToken);
                if (detail != null)
                {
                    await _realtimeService.SendGroupRequestUpdatedAsync(offer.GroupRequestId, detail);
                }
            }
            catch
            {
                // Non-blocking SignalR fallback
            }

            // 5. Fire notification created events
            foreach (var notification in notifications)
            {
                await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), cancellationToken);
            }

            return true;
        }
    }
}
