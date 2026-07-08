using Jomla.Application.Features.GroupRequests.Commands.JoinGroupRequest;
using Jomla.Application.Features.GroupRequests.Commands.ReactivateGroupRequest;
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

public class JoinGroupRequestCommandHandlerTests : ApplicationTestBase
{
    private readonly JoinGroupRequestCommandHandler _handler;

    public JoinGroupRequestCommandHandlerTests()
    {
        _handler = new JoinGroupRequestCommandHandler(
            Context,
            JobDispatcher,
            Mediator,
            RealtimeService);
    }

    [Fact]
    public async Task Handle_GroupRequestNotFound_ReturnsError()
    {
        // Arrange
        var command = new JoinGroupRequestCommand(Guid.NewGuid(), Guid.NewGuid(), 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Group request not found.", result.Error);
    }

    [Fact]
    public async Task Handle_GroupRequestClosed_ReturnsError()
    {
        // Arrange
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Closed,
            ModerationStatus = ModerationStatus.Approved
        };
        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new JoinGroupRequestCommand(groupRequest.Id, Guid.NewGuid(), 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Group request is closed.", result.Error);
    }

    [Fact]
    public async Task Handle_GroupRequestFlagged_ReturnsError()
    {
        // Arrange
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Active,
            ModerationStatus = ModerationStatus.Flagged
        };
        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new JoinGroupRequestCommand(groupRequest.Id, Guid.NewGuid(), 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Group request is flagged.", result.Error);
    }

    [Fact]
    public async Task Handle_GroupRequestNotApprovedYet_ReturnsError()
    {
        // Arrange
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Active,
            ModerationStatus = ModerationStatus.Pending
        };
        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new JoinGroupRequestCommand(groupRequest.Id, Guid.NewGuid(), 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Group request is not approved yet.", result.Error);
    }

    [Fact]
    public async Task Handle_AlreadyJoined_ReturnsError()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
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

        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        await Context.SaveChangesAsync();

        var command = new JoinGroupRequestCommand(groupRequest.Id, buyerId, 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You already joined this group request.", result.Error);
    }

    [Fact]
    public async Task Handle_Rejoin_UpdatesExistingRecord()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Active,
            ModerationStatus = ModerationStatus.Approved,
            CurrentQuantity = 10
        };
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId,
            Status = GroupRequestParticipantStatus.Left,
            Quantity = 3
        };

        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        await Context.SaveChangesAsync();

        var command = new JoinGroupRequestCommand(groupRequest.Id, buyerId, 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        
        var updatedParticipant = await Context.GroupRequestParticipants
            .FirstOrDefaultAsync(p => p.GroupRequestId == groupRequest.Id && p.BuyerId == buyerId);
        
        Assert.NotNull(updatedParticipant);
        Assert.Equal(GroupRequestParticipantStatus.Active, updatedParticipant.Status);
        Assert.Equal(5, updatedParticipant.Quantity);

        var updatedGroupRequest = await Context.GroupRequests.FindAsync(groupRequest.Id);
        Assert.NotNull(updatedGroupRequest);
        Assert.Equal(15, updatedGroupRequest.CurrentQuantity); // 10 + 5
    }

    [Fact]
    public async Task Handle_JoinSuccessActiveRequest_AddsParticipantAndEnqueuesJob()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Active,
            ModerationStatus = ModerationStatus.Approved,
            CurrentQuantity = 10,
            CategoryId = Guid.NewGuid()
        };

        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new JoinGroupRequestCommand(groupRequest.Id, buyerId, 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        var updatedParticipant = await Context.GroupRequestParticipants
            .FirstOrDefaultAsync(p => p.GroupRequestId == groupRequest.Id && p.BuyerId == buyerId);
        
        Assert.NotNull(updatedParticipant);
        Assert.Equal(GroupRequestParticipantStatus.Active, updatedParticipant.Status);
        Assert.Equal(5, updatedParticipant.Quantity);

        var updatedGroupRequest = await Context.GroupRequests.FindAsync(groupRequest.Id);
        Assert.NotNull(updatedGroupRequest);
        Assert.Equal(15, updatedGroupRequest.CurrentQuantity);

        // Verifying BackgroundJobDispatcher triggers matching job
        JobDispatcher.Received(1).Enqueue(Arg.Any<Expression<Func<ISupplierMatchingJob, Task>>>());
    }

    [Fact]
    public async Task Handle_JoinSuccessInactiveRequest_DispatchesReactivateCommand()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Inactive,
            ModerationStatus = ModerationStatus.Approved,
            CurrentQuantity = 10
        };

        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new JoinGroupRequestCommand(groupRequest.Id, buyerId, 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        // ReactivateGroupRequestCommand should be sent, which enqueues its own job
        await Mediator.Received(1).Send(Arg.Is<ReactivateGroupRequestCommand>(c => c.GroupRequestId == groupRequest.Id), Arg.Any<CancellationToken>());
        
        // BackgroundJobDispatcher should NOT be directly enqueued here (delegated to reactivate command)
        JobDispatcher.DidNotReceiveWithAnyArgs().Enqueue(Arg.Any<Expression<Func<ISupplierMatchingJob, Task>>>());
    }

    [Fact]
    public async Task Handle_GroupRequestFulfilled_ReturnsError()
    {
        // Arrange
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Fulfilled,
            ModerationStatus = ModerationStatus.Approved
        };

        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new JoinGroupRequestCommand(groupRequest.Id, Guid.NewGuid(), 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Group request has already been fulfilled.", result.Error);
    }
}
