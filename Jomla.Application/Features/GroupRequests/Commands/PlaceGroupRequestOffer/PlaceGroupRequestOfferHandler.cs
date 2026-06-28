using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Expiry;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Jomla.Application.Features.GroupRequests.Commands.PlaceGroupRequestOffer;

public sealed class PlaceGroupRequestOfferHandler(IAppDbContext db,UserManager<AppUser> userManager, IBackgroundJobDispatcher backgroundJobDispatcher) : IRequestHandler<PlaceGroupRequestOfferCommand, Guid>
{
    public async Task<Guid> Handle(PlaceGroupRequestOfferCommand request,CancellationToken cancellationToken)
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

        var groupRequest = await db.GroupRequests.Include(x => x.Participants).Include(x => x.Alerts).FirstOrDefaultAsync(
                 x => x.Id == request.GroupRequestId,cancellationToken);
        if (groupRequest is null)
            throw new NotFoundException(nameof(GroupRequest), request.GroupRequestId);


        if (groupRequest.Status != GroupRequestStatus.Active)
        {
            throw new ValidationException("This group request is not active.");
        }

        if (groupRequest.ModerationStatus != ModerationStatus.Approved)
        {
            throw new ValidationException("This group request is not approved.");
        }

        if (groupRequest.InitiatorId == supplierId)
        {
            throw new ValidationException("You cannot place an offer on your own group request.");
        }

        if (groupRequest.InactiveSince.HasValue)
        {
            throw new ValidationException("This group request is inactive.");
        }


        var participant = groupRequest.Participants .FirstOrDefault(x => x.BuyerId == supplierId);

        if (participant is not null)
        {
            throw new ValidationException("Suppliers cannot join as participants.");
        }

        var alreadyPlaced = await db.GroupRequestOffers.AnyAsync(
        x =>
            x.GroupRequestId == request.GroupRequestId &&
            x.SupplierId == supplierId &&
            x.Status == GroupRequestOfferStatus.Open,
        cancellationToken);

        if (alreadyPlaced)
            throw new ConflictException("You already have an active offer.");


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

        
        foreach (var participan in groupRequest.Participants)
        {
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),

                UserId = participan.BuyerId,

                Type = NotificationType.GroupRequestOfferPlaced,

                Title = "New Offer Placed",

                Body =
                    $"{supplier.FirstName} {supplier.LastName} placed a new offer on a group request you're participating in.",

                EntityId = offer.Id,

                EntityType = nameof(GroupRequestOffer),

                IsRead = false,

                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var jobId = backgroundJobDispatcher.Schedule<IGroupRequestOfferExpiryJob>(x => x.ExcuteAsync(offer.Id),offer.ExpiresAt);

        offer.JobId = jobId;

        await db.SaveChangesAsync(cancellationToken);

        return offer.Id;
    }
}