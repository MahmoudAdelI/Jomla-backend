using Jomla.Application.Features.GroupRequests.Commands.ModerateGroupRequest;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Matching;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.GroupRequests;

public class ModerateGroupRequestCommandHandlerTests : ApplicationTestBase
{
    private readonly IModerationAgent _moderationAgent;
    private readonly ModerateGroupRequestCommandHandler _handler;

    public ModerateGroupRequestCommandHandlerTests()
    {
        _moderationAgent = Substitute.For<IModerationAgent>();
        _handler = new ModerateGroupRequestCommandHandler(
            Context,
            _moderationAgent,
            Mediator,
            JobDispatcher,
            RealtimeService);
    }

    [Fact]
    public async Task Handle_GroupRequestNotFound_ReturnsSilently()
    {
        // Arrange
        var command = new ModerateGroupRequestCommand(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = _moderationAgent.DidNotReceiveWithAnyArgs().ModerateAsync(Arg.Any<ModerationInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GroupRequestNotPending_ReturnsSilently()
    {
        // Arrange
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Active,
            ModerationStatus = ModerationStatus.Approved
        };
        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new ModerateGroupRequestCommand(groupRequest.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = _moderationAgent.DidNotReceiveWithAnyArgs().ModerateAsync(Arg.Any<ModerationInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ModerationApproved_SetsStatusActiveAndEnqueuesMatchingJob()
    {
        // Arrange
        var initiatorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            CategoryId = categoryId,
            CurrentQuantity = 10,
            Status = GroupRequestStatus.PendingReview,
            ModerationStatus = ModerationStatus.Pending
        };
        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        _moderationAgent.ModerateAsync(Arg.Any<ModerationInput>(), Arg.Any<CancellationToken>())
            .Returns(new ModerationResult(true, "Looks clean"));

        var command = new ModerateGroupRequestCommand(groupRequest.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedRequest = await Context.GroupRequests.FindAsync(groupRequest.Id);
        Assert.NotNull(updatedRequest);
        Assert.Equal(GroupRequestStatus.Active, updatedRequest.Status);
        Assert.Equal(ModerationStatus.Approved, updatedRequest.ModerationStatus);

        // Verify initiator added as participant
        var participant = await Context.GroupRequestParticipants
            .FirstOrDefaultAsync(p => p.GroupRequestId == groupRequest.Id && p.BuyerId == initiatorId);
        Assert.NotNull(participant);
        Assert.Equal(GroupRequestParticipantStatus.Active, participant.Status);
        Assert.Equal(10, participant.Quantity);

        // Verify ISupplierMatchingJob enqueued
        JobDispatcher.Received(1).Enqueue(Arg.Any<Expression<Func<ISupplierMatchingJob, Task>>>());

        // Verify notification
        var notifications = await Context.Notifications.ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(initiatorId, notifications[0].UserId);
        Assert.Equal(NotificationType.GroupRequestApproved, notifications[0].Type);
    }

    [Fact]
    public async Task Handle_ModerationFlagged_SetsStatusInactiveAndNotifiesInitiator()
    {
        // Arrange
        var initiatorId = Guid.NewGuid();
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            Status = GroupRequestStatus.PendingReview,
            ModerationStatus = ModerationStatus.Pending
        };
        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        _moderationAgent.ModerateAsync(Arg.Any<ModerationInput>(), Arg.Any<CancellationToken>())
            .Returns(new ModerationResult(false, "Inappropriate content"));

        var command = new ModerateGroupRequestCommand(groupRequest.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedRequest = await Context.GroupRequests.FindAsync(groupRequest.Id);
        Assert.NotNull(updatedRequest);
        Assert.Equal(GroupRequestStatus.Inactive, updatedRequest.Status);
        Assert.Equal(ModerationStatus.Flagged, updatedRequest.ModerationStatus);
        Assert.Equal("Inappropriate content", updatedRequest.ModerationReason);

        // No matching job should be enqueued
        JobDispatcher.DidNotReceiveWithAnyArgs().Enqueue(Arg.Any<Expression<Func<ISupplierMatchingJob, Task>>>());

        // Verify notification
        var notifications = await Context.Notifications.ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(NotificationType.GroupRequestFlagged, notifications[0].Type);

        // Verify Realtime flagged created event sent
        await RealtimeService.Received(1).SendFlaggedItemCreatedAsync("GroupRequest", groupRequest.Id);
    }
}
