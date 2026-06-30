using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.Commands.CreateBatch;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Commands.ApproveOffer
{
    public sealed class ApproveOfferCommandHandler : IRequestHandler<ApproveOfferCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IMediator _mediator;
        private readonly IBackgroundJobDispatcher _jobDispatcher;

        public ApproveOfferCommandHandler(IAppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task Handle(ApproveOfferCommand request, CancellationToken cancellationToken)
        {
            var offer = await _context.SupplierOffers
                .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken);

            if (offer == null || offer.ModerationStatus != ModerationStatus.Flagged)
                return;

            offer.ModerationStatus = ModerationStatus.Approved;
            offer.Status = SupplierOfferStatus.Active;
            offer.ModerationReason = null;

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = offer.SupplierId,
                Type = NotificationType.OfferApproved,
                Title = "Your offer has been approved",
                Body = "Your offer has been reviewed and approved by our team.",
                EntityId = offer.Id,
                EntityType = nameof(SupplierOffer),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new CreateBatchCommand(offer.Id), cancellationToken);
            await _mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), cancellationToken);
        }
    }
}
