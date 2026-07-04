using Jomla.Application.Features.GroupRequests.Commands.LeaveGroupRequest;
using Jomla.Application.Features.GroupRequests.Commands.CancelGroupRequestOffer;
using Jomla.Application.Jobs.Closing;
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

public class LeaveGroupRequestCommandHandlerTests : ApplicationTestBase
{
    private readonly LeaveGroupRequestCommandHandler _handler;

    public LeaveGroupRequestCommandHandlerTests()
    {
        _handler = new LeaveGroupRequestCommandHandler(
            Context,
            JobDispatcher,
            Mediator,
            RealtimeService);
    }

    [Fact]
    public async Task Handle_GroupRequestNotFound_ReturnsError()
    {
        // Arrange
        var command = new LeaveGroupRequestCommand(Guid.NewGuid(), Guid.NewGuid());

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
            Status = GroupRequestStatus.Closed
        };
        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new LeaveGroupRequestCommand(groupRequest.Id, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Group request is closed.", result.Error);
    }

    [Fact]
    public async Task Handle_NotMemberOfGroupRequest_ReturnsError()
    {
        // Arrange
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Active
        };
        Context.GroupRequests.Add(groupRequest);
        await Context.SaveChangesAsync();

        var command = new LeaveGroupRequestCommand(groupRequest.Id, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You are not a member of this group request.", result.Error);
    }

    [Fact]
    public async Task Handle_AcceptedOfferIsProcessing_ReturnsError()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Active
        };
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId,
            Status = GroupRequestParticipantStatus.Active,
            Quantity = 5
        };
        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            Status = GroupRequestOfferStatus.Accepted
        };
        var response = new BuyerOfferResponse
        {
            OfferId = offer.Id,
            Offer = offer,
            BuyerId = buyerId,
            Response = BuyerOfferResponseType.Accepted
        };

        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        Context.GroupRequestOffers.Add(offer);
        Context.BuyerOfferResponses.Add(response);
        await Context.SaveChangesAsync();

        var command = new LeaveGroupRequestCommand(groupRequest.Id, buyerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Cannot leave the group request while one of your accepted offers is being processed.", result.Error);
    }

    [Fact]
    public async Task Handle_OpenAcceptedOffers_CancelsThemAndLeavesSuccessfully()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Active,
            CurrentQuantity = 10
        };
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId,
            Status = GroupRequestParticipantStatus.Active,
            Quantity = 4
        };
        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            Status = GroupRequestOfferStatus.Open
        };
        var response = new BuyerOfferResponse
        {
            OfferId = offer.Id,
            Offer = offer,
            BuyerId = buyerId,
            Response = BuyerOfferResponseType.Accepted
        };

        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        Context.GroupRequestOffers.Add(offer);
        Context.BuyerOfferResponses.Add(response);
        await Context.SaveChangesAsync();

        var command = new LeaveGroupRequestCommand(groupRequest.Id, buyerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        // Check participant updated
        var updatedParticipant = await Context.GroupRequestParticipants
            .FirstOrDefaultAsync(p => p.GroupRequestId == groupRequest.Id && p.BuyerId == buyerId);
        Assert.NotNull(updatedParticipant);
        Assert.Equal(GroupRequestParticipantStatus.Left, updatedParticipant.Status);

        // Check group request quantity decremented
        var updatedGroupRequest = await Context.GroupRequests.FindAsync(groupRequest.Id);
        Assert.NotNull(updatedGroupRequest);
        Assert.Equal(6, updatedGroupRequest.CurrentQuantity); // 10 - 4

        // Verify CancelGroupRequestOfferCommand was sent
        await Mediator.Received(1).Send(Arg.Is<CancelGroupRequestOfferCommand>(c => c.OfferId == offer.Id && c.BuyerId == buyerId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LeaveEmptyRequest_SetsInactiveAndSchedulesAutoCloseJob()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Status = GroupRequestStatus.Active,
            CurrentQuantity = 4
        };
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId,
            Status = GroupRequestParticipantStatus.Active,
            Quantity = 4
        };

        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        await Context.SaveChangesAsync();

        var command = new LeaveGroupRequestCommand(groupRequest.Id, buyerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        var updatedGroupRequest = await Context.GroupRequests.FindAsync(groupRequest.Id);
        Assert.NotNull(updatedGroupRequest);
        Assert.Equal(0, updatedGroupRequest.CurrentQuantity);
        Assert.Equal(GroupRequestStatus.Inactive, updatedGroupRequest.Status);
        Assert.NotNull(updatedGroupRequest.InactiveSince);

        // Verify Schedule AutoCloseJob
        JobDispatcher.Received(1).Schedule(
            Arg.Any<Expression<Func<IGroupRequestAutoCloseJob, Task>>>(),
            Arg.Any<DateTimeOffset>());
    }
}
