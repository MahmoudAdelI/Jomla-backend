using Jomla.Application.Features.GroupRequests.Commands.PlaceGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Queries;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Expiry;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.GroupRequests;

public class PlaceGroupRequestOfferHandlerTests : ApplicationTestBase
{
    private readonly PlaceGroupRequestOfferHandler _handler;

    public PlaceGroupRequestOfferHandlerTests()
    {
        _handler = new PlaceGroupRequestOfferHandler(
            Context,
            IdentityService,
            JobDispatcher,
            Mediator,
            RealtimeService);
    }

    [Fact]
    public async Task Handle_SupplierNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        IdentityService.FindByIdAsync(supplierId).Returns((AppUser)null!);

        var command = new PlaceGroupRequestOfferCommand
        {
            SupplierId = supplierId,
            GroupRequestId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotASupplierRole_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var user = new AppUser { Id = supplierId };
        IdentityService.FindByIdAsync(supplierId).Returns(user);
        IdentityService.IsInRoleAsync(user, nameof(UserRole.Supplier)).Returns(false);

        var command = new PlaceGroupRequestOfferCommand
        {
            SupplierId = supplierId,
            GroupRequestId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GroupRequestNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var user = new AppUser { Id = supplierId };
        IdentityService.FindByIdAsync(supplierId).Returns(user);
        IdentityService.IsInRoleAsync(user, nameof(UserRole.Supplier)).Returns(true);

        var command = new PlaceGroupRequestOfferCommand
        {
            SupplierId = supplierId,
            GroupRequestId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GroupRequestClosed_ThrowsConflictException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var user = new AppUser { Id = supplierId };
        IdentityService.FindByIdAsync(supplierId).Returns(user);
        IdentityService.IsInRoleAsync(user, nameof(UserRole.Supplier)).Returns(true);

        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Status = GroupRequestStatus.Closed };
        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new PlaceGroupRequestOfferCommand
        {
            SupplierId = supplierId,
            GroupRequestId = groupRequest.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GroupRequestFlagged_ThrowsBadRequestException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var user = new AppUser { Id = supplierId };
        IdentityService.FindByIdAsync(supplierId).Returns(user);
        IdentityService.IsInRoleAsync(user, nameof(UserRole.Supplier)).Returns(true);

        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Active,
            ModerationStatus = ModerationStatus.Flagged
        };
        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new PlaceGroupRequestOfferCommand
        {
            SupplierId = supplierId,
            GroupRequestId = groupRequest.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OwnGroupRequest_ThrowsBadRequestException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var user = new AppUser { Id = supplierId };
        IdentityService.FindByIdAsync(supplierId).Returns(user);
        IdentityService.IsInRoleAsync(user, nameof(UserRole.Supplier)).Returns(true);

        // Supplier is the initiator of the request
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            InitiatorId = supplierId,
            Status = GroupRequestStatus.Active,
            ModerationStatus = ModerationStatus.Approved
        };
        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new PlaceGroupRequestOfferCommand
        {
            SupplierId = supplierId,
            GroupRequestId = groupRequest.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyHasActiveOffer_ThrowsConflictException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var user = new AppUser { Id = supplierId };
        IdentityService.FindByIdAsync(supplierId).Returns(user);
        IdentityService.IsInRoleAsync(user, nameof(UserRole.Supplier)).Returns(true);

        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            InitiatorId = Guid.NewGuid(),
            Status = GroupRequestStatus.Active,
            ModerationStatus = ModerationStatus.Approved
        };
        var existingOffer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            SupplierId = supplierId,
            Status = GroupRequestOfferStatus.Open
        };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestOffers.Add(existingOffer);
        await Context.SaveChangesAsync();

        var command = new PlaceGroupRequestOfferCommand
        {
            SupplierId = supplierId,
            GroupRequestId = groupRequest.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_SavesOfferUpdatesAlertAndDispatchesNotifications()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var supplier = new AppUser { Id = supplierId, FirstName = "Bob", LastName = "Builder" };
        IdentityService.FindByIdAsync(supplierId).Returns(supplier);
        IdentityService.IsInRoleAsync(supplier, nameof(UserRole.Supplier)).Returns(true);

        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            InitiatorId = Guid.NewGuid(),
            Status = GroupRequestStatus.Active,
            ModerationStatus = ModerationStatus.Approved
        };
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId,
            Status = GroupRequestParticipantStatus.Active,
            Quantity = 3
        };
        var alert = new GroupRequestAlert
        {
            GroupRequestId = groupRequest.Id,
            SupplierId = supplierId,
            Status = GroupRequestAlertStatus.Pending
        };

        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        Context.GroupRequestAlerts.Add(alert);
        await Context.SaveChangesAsync();

        var expiresAt = DateTimeOffset.UtcNow.AddDays(5);
        var command = new PlaceGroupRequestOfferCommand
        {
            SupplierId = supplierId,
            GroupRequestId = groupRequest.Id,
            UnitPrice = 25m,
            MinUnitPrice = 20m,
            QuantityAvailable = 100,
            MinFallbackQuantity = 10,
            VariantAttributes = "{}",
            ExpiresAt = expiresAt
        };

        JobDispatcher.Schedule(Arg.Any<Expression<Func<IGroupRequestOfferExpiryJob, Task>>>(), expiresAt)
            .Returns("exp_job_offer_999");

        // Act
        var resultOfferId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultOfferId);

        var savedOffer = await Context.GroupRequestOffers.FindAsync(resultOfferId);
        Assert.NotNull(savedOffer);
        Assert.Equal(GroupRequestOfferStatus.Open, savedOffer.Status);
        Assert.Equal("exp_job_offer_999", savedOffer.JobId);
        Assert.Equal(25m, savedOffer.UnitPrice);

        // Verify alert status updated
        var updatedAlert = await Context.GroupRequestAlerts.FindAsync(groupRequest.Id, supplierId);
        Assert.NotNull(updatedAlert);
        Assert.Equal(GroupRequestAlertStatus.Responded, updatedAlert.Status);

        // Verify notifications created
        var notifications = await Context.Notifications.ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(buyerId, notifications[0].UserId);
        Assert.Equal(NotificationType.GroupRequestOfferPlaced, notifications[0].Type);

        // Verify real-time notification publication
        await Mediator.Received(1).Publish(Arg.Is<NotificationCreatedEvent>(e => e.UserId == buyerId), Arg.Any<CancellationToken>());
    }
}
