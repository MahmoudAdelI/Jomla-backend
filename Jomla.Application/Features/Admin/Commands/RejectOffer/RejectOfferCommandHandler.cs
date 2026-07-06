using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Features.Offers.Queries.GetOfferById;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Commands.RejectOffer
{
    public sealed class RejectOfferCommandHandler : IRequestHandler<RejectOfferCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IMediator _mediator;
        private readonly IRealtimeService _realtimeService;

        public RejectOfferCommandHandler(
            IAppDbContext context, 
            IMediator mediator,
            IRealtimeService realtimeService)
        {
            _context = context;
            _mediator = mediator;
            _realtimeService = realtimeService;
        }

        public async Task Handle(RejectOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _context.SupplierOffers
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer == null ||
                 (offer.ModerationStatus != ModerationStatus.Flagged &&
                  offer.ModerationStatus != ModerationStatus.Pending))
                return;

            offer.ModerationStatus = ModerationStatus.Flagged;
            offer.ModerationReason = request.Reason;

            var notification = new Notification
            {
                UserId = offer.SupplierId,
                Type = NotificationType.OfferFlagged,
                Title = "Your offer has been rejected",
                Body = $"Your offer was rejected: {request.Reason}",
                EntityId = offer.Id,
                EntityType = nameof(SupplierOffer),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                var offerDto = await _mediator.Send(new GetOfferByIdQuery(offer.Id), cancellationToken);
                if (offerDto != null)
                {
                    await _realtimeService.SendOfferStatusChangedAsync(offer.SupplierId, offerDto);
                }
                await _realtimeService.SendFlaggedItemResolvedAsync(offer.Id);
            }
            catch
            {
                // Non-blocking SignalR fallback
            }

            await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), cancellationToken);
        }
    }
}
