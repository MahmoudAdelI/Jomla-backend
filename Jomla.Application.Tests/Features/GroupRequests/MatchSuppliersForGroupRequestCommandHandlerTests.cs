using Jomla.Application.Features.GroupRequests.Commands.MatchSuppliersForGroupRequest;
using Jomla.Application.Features.Notifications;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.GroupRequests;

public class MatchSuppliersForGroupRequestCommandHandlerTests : ApplicationTestBase
{
    private readonly MatchSuppliersForGroupRequestCommandHandler _handler;

    public MatchSuppliersForGroupRequestCommandHandlerTests()
    {
        _handler = new MatchSuppliersForGroupRequestCommandHandler(Context, Mediator);
    }

    [Fact]
    public async Task Handle_NoMatchesFound_DoesNotInsertOrNotify()
    {
        // Arrange
        var command = new MatchSuppliersForGroupRequestCommand(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var alerts = await Context.GroupRequestAlerts.ToListAsync();
        Assert.Empty(alerts);

        var notifications = await Context.Notifications.ToListAsync();
        Assert.Empty(notifications);

        await Mediator.DidNotReceiveWithAnyArgs().Publish(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MatchesFound_InsertsAlertsNotificationsAndPublishesEvents()
    {
        // Arrange
        var groupRequestId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var supplier1 = Guid.NewGuid(); // Matches everything
        var supplier2 = Guid.NewGuid(); // Already alerted (should be ignored)
        var supplier3 = Guid.NewGuid(); // Quantity threshold too high (should be ignored)
        var supplier4 = Guid.NewGuid(); // Different category (should be ignored)

        // Preferences
        var p1 = new SupplierCategoryPreference { SupplierId = supplier1, CategoryId = categoryId, MinQuantity = 5 };
        var p2 = new SupplierCategoryPreference { SupplierId = supplier2, CategoryId = categoryId, MinQuantity = 5 };
        var p3 = new SupplierCategoryPreference { SupplierId = supplier3, CategoryId = categoryId, MinQuantity = 15 }; // 15 > 10
        var p4 = new SupplierCategoryPreference { SupplierId = supplier4, CategoryId = Guid.NewGuid(), MinQuantity = 5 };

        // Existing alert
        var existingAlert = new GroupRequestAlert
        {
            SupplierId = supplier2,
            GroupRequestId = groupRequestId,
            Status = GroupRequestAlertStatus.Pending
        };

        Context.SupplierCategoryPreferences.AddRange(p1, p2, p3, p4);
        Context.GroupRequestAlerts.Add(existingAlert);
        await Context.SaveChangesAsync();

        var command = new MatchSuppliersForGroupRequestCommand(groupRequestId, categoryId, 10);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Verify alerts: should contain the existing one + the new one for supplier1
        var alerts = await Context.GroupRequestAlerts.ToListAsync();
        Assert.Equal(2, alerts.Count);
        Assert.Contains(alerts, a => a.SupplierId == supplier1 && a.GroupRequestId == groupRequestId);

        // Verify notifications: only supplier1 should get a notification
        var notifications = await Context.Notifications.ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(supplier1, notifications[0].UserId);
        Assert.Equal(NotificationType.GroupRequestMatched, notifications[0].Type);

        // Verify Mediator published NotificationCreatedEvent for supplier1
        await Mediator.Received(1).Publish(
            Arg.Is<NotificationCreatedEvent>(e => e.UserId == supplier1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MatchSubcategory_ParentPreferenceMatched_InsertsAlertAndNotifies()
    {
        // Arrange
        var groupRequestId = Guid.NewGuid();
        var parentCategoryId = Guid.NewGuid();
        var subCategoryId = Guid.NewGuid();

        var parentCategory = new Category { Id = parentCategoryId, Name = "Parent" };
        var subCategory = new Category { Id = subCategoryId, Name = "Sub", ParentId = parentCategoryId };

        var supplier = Guid.NewGuid();

        // Supplier has preference on parent category
        var p = new SupplierCategoryPreference { SupplierId = supplier, CategoryId = parentCategoryId, MinQuantity = 5 };

        Context.Categories.AddRange(parentCategory, subCategory);
        Context.SupplierCategoryPreferences.Add(p);
        await Context.SaveChangesAsync();

        var command = new MatchSuppliersForGroupRequestCommand(groupRequestId, subCategoryId, 10);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var alerts = await Context.GroupRequestAlerts.ToListAsync();
        Assert.Single(alerts);
        Assert.Equal(supplier, alerts[0].SupplierId);

        var notifications = await Context.Notifications.ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(supplier, notifications[0].UserId);

        await Mediator.Received(1).Publish(
            Arg.Is<NotificationCreatedEvent>(e => e.UserId == supplier),
            Arg.Any<CancellationToken>());
    }
}
