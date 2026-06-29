using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Jobs.Expiry;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Jomla.Application.Features.GroupRequests.Commands.PlaceGroupRequestOffer;

public sealed class PlaceGroupRequestOfferHandler(
    IAppDbContext db,
    UserManager<AppUser> userManager,
    IBackgroundJobDispatcher backgroundJobDispatcher,
    IMediator mediator) : IRequestHandler<PlaceGroupRequestOfferCommand, Guid>
{
    public async Task<Guid> Handle(PlaceGroupRequestOfferCommand request, CancellationToken cancellationToken)
    {
        var supplierId = request.SupplierId;

        var supplier = await userManager.Users
            .FirstOrDefaultAsync(
                x => x.Id == supplierId,
                cancellationToken);

        if (supplier is null)
            throw new NotFoundException(nameof(AppUser), supplierId);

        if (!await userManager.IsInRoleAsync(supplier, nameof(UserRole.Supplier)))
            throw new UnauthorizedAccessException();

        var groupRequest = await db.GroupRequests
            .Include(x => x.Participants)
            .Include(x => x.Alerts)
            .FirstOrDefaultAsync(x => x.Id == request.GroupRequestId, cancellationToken);

        if (groupRequest is null)
            throw new NotFoundException(nameof(GroupRequest), request.GroupRequestId);

        // Core business rule: Block offers on closed requests, but allow on active or inactive requests.
        if (groupRequest.Status == GroupRequestStatus.Closed)
        {
            throw new ConflictException("Cannot place offers on a closed group request.");
        }

        if (groupRequest.ModerationStatus == ModerationStatus.Flagged)
        {
            throw new ValidationException("This group request is flagged.");
        }

        if (groupRequest.ModerationStatus != ModerationStatus.Approved)
        {
            throw new ValidationException("This group request is not approved yet.");
        }

        if (groupRequest.InitiatorId == supplierId)
        {
            throw new ValidationException("You cannot place an offer on your own group request.");
        }

        var participant = groupRequest.Participants.FirstOrDefault(x => x.BuyerId == supplierId);
        if (participant is not null)
        {
            throw new ValidationException("Suppliers cannot join as participants.");
        }

        // Idempotency: Check if the supplier already has an active, Open offer on this Group Request
        var alreadyPlaced = await db.GroupRequestOffers.AnyAsync(
            x => x.GroupRequestId == request.GroupRequestId &&
                 x.SupplierId == supplierId &&
                 x.Status == GroupRequestOfferStatus.Open,
            cancellationToken);

        if (alreadyPlaced)
            throw new ConflictException("You already have an active offer.");

        // Update the GroupRequestAlert record if it exists
        var alert = await db.GroupRequestAlerts
            .FirstOrDefaultAsync(x => x.GroupRequestId == request.GroupRequestId && x.SupplierId == supplierId, cancellationToken);
        if (alert is not null && alert.Status != GroupRequestAlertStatus.Responded)
        {
            alert.Status = GroupRequestAlertStatus.Responded;
        }

        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            SupplierId = supplierId,
            UnitPrice = request.UnitPrice,
            CurrentUnitPrice = request.UnitPrice,
            MinUnitPrice = request.MinUnitPrice,
            QuantityAvailable = request.QuantityAvailable,
            MinFallbackQuantity = request.MinFallbackQuantity,
            VariantAttributes = request.VariantAttributes,
            AcceptedQuantity = 0,
            RoundNumber = 1,
            Status = GroupRequestOfferStatus.Open,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt,
            JobId = string.Empty
        };

        db.GroupRequestOffers.Add(offer);

        // Notify active participants about the new offer
        var notifications = new List<Notification>();
        var activeParticipants = groupRequest.Participants
            .Where(p => p.Status == GroupRequestParticipantStatus.Active)
            .ToList();

        foreach (var activeParticipant in activeParticipants)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = activeParticipant.BuyerId,
                Type = NotificationType.GroupRequestOfferPlaced,
                Title = "New Offer Placed",
                Body = $"{supplier.FirstName} {supplier.LastName} placed a new offer on a group request you're participating in.",
                EntityId = groupRequest.Id,
                EntityType = nameof(GroupRequest),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            db.Notifications.Add(notification);
            notifications.Add(notification);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Schedule the offer expiry job
        var jobId = backgroundJobDispatcher.Schedule<IGroupRequestOfferExpiryJob>(x => x.ExcuteAsync(offer.Id), offer.ExpiresAt);
        offer.JobId = jobId;

        await db.SaveChangesAsync(cancellationToken);

        // Trigger real-time SignalR notifications
        foreach (var notification in notifications)
        {
            await mediator.Publish(new NotificationCreatedEvent(notification.UserId, notification.Id), cancellationToken);
        }

        return offer.Id;
    }
}