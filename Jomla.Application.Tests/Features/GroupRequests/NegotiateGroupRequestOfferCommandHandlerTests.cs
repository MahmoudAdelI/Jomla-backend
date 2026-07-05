using Jomla.Application.Features.GroupRequests.Commands.NegotiateGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Queries;
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

public class NegotiateGroupRequestOfferCommandHandlerTests : ApplicationTestBase
{
    private readonly INegotiationAgent _negotiationAgent;
    private readonly NegotiateGroupRequestOfferCommandHandler _handler;

    public NegotiateGroupRequestOfferCommandHandlerTests()
    {
        _negotiationAgent = Substitute.For<INegotiationAgent>();
        _handler = new NegotiateGroupRequestOfferCommandHandler(
            Context,
            _negotiationAgent,
            Mediator,
            JobDispatcher,
            RealtimeService);
    }

    [Fact]
    public async Task Handle_OfferNotFound_ReturnsSilently()
    {
        // Arrange
        var command = new NegotiateGroupRequestOfferCommand(Guid.NewGuid(), "Vegetables");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _negotiationAgent.DidNotReceiveWithAnyArgs().GetNextPriceAsync(Arg.Any<GroupRequestOffer>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Success_CreatesCounterOfferSupercedesParentResponsesAndSendsNotifications()
    {
        // Arrange
        var buyerId1 = Guid.NewGuid();
        var buyerId2 = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        var groupRequest = new GroupRequest
        {
            Id = Guid.NewGuid(),
            Title = "Tomato Group",
            Status = GroupRequestStatus.Active,
            CurrentQuantity = 10
        };

        var p1 = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId1,
            Status = GroupRequestParticipantStatus.Active,
            Quantity = 4
        };
        var p2 = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId2,
            Status = GroupRequestParticipantStatus.Active,
            Quantity = 6
        };

        var parentOffer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            GroupRequest = groupRequest,
            SupplierId = supplierId,
            UnitPrice = 50m,
            CurrentUnitPrice = 50m,
            QuantityAvailable = 20,
            RoundNumber = 1,
            Status = GroupRequestOfferStatus.Open,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };

        var parentResponse1 = new BuyerOfferResponse
        {
            OfferId = parentOffer.Id,
            Offer = parentOffer,
            BuyerId = buyerId1,
            Response = BuyerOfferResponseType.Accepted,
            StripePaymentIntentId = "pi_buyer1"
        };
        var parentResponse2 = new BuyerOfferResponse
        {
            OfferId = parentOffer.Id,
            Offer = parentOffer,
            BuyerId = buyerId2,
            Response = BuyerOfferResponseType.Accepted,
            StripePaymentIntentId = "pi_buyer2"
        };

        parentOffer.Responses.Add(parentResponse1);
        parentOffer.Responses.Add(parentResponse2);
        groupRequest.Participants.Add(p1);
        groupRequest.Participants.Add(p2);

        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestOffers.Add(parentOffer);
        Context.BuyerOfferResponses.Add(parentResponse1);
        Context.BuyerOfferResponses.Add(parentResponse2);
        await Context.SaveChangesAsync();

        _negotiationAgent.GetNextPriceAsync(Arg.Any<GroupRequestOffer>(), "Vegetables")
            .Returns(45m); // AI counters with 45 dollars

        JobDispatcher.Schedule(Arg.Any<Expression<Func<IGroupRequestOfferExpiryJob, Task>>>(), Arg.Any<DateTimeOffset>())
            .Returns("child_exp_job_123");

        var command = new NegotiateGroupRequestOfferCommand(parentOffer.Id, "Vegetables");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Verify parent offer status countered
        var updatedParent = await Context.GroupRequestOffers.FindAsync(parentOffer.Id);
        Assert.NotNull(updatedParent);
        Assert.Equal(GroupRequestOfferStatus.Countered, updatedParent.Status);

        // Verify parent responses superceded
        var responses = await Context.BuyerOfferResponses.ToListAsync();
        var movedResponses = responses.Where(r => r.OfferId == parentOffer.Id).ToList();
        Assert.Equal(2, movedResponses.Count);
        Assert.All(movedResponses, r => Assert.Equal(BuyerOfferResponseType.MovedToNextRound, r.Response));

        // Verify child offer was created
        var allOffers = await Context.GroupRequestOffers.ToListAsync();
        var childOffer = allOffers.FirstOrDefault(o => o.ParentId == parentOffer.Id);
        Assert.NotNull(childOffer);
        Assert.Equal(GroupRequestOfferStatus.Open, childOffer.Status);
        Assert.Equal(45m, childOffer.CurrentUnitPrice);
        Assert.Equal(2, childOffer.RoundNumber);
        Assert.Equal("child_exp_job_123", childOffer.JobId);

        // Verify child responses created
        var childResponses = responses.Where(r => r.OfferId == childOffer.Id).ToList();
        Assert.Equal(2, childResponses.Count);
        Assert.Contains(childResponses, r => r.BuyerId == buyerId1 && r.Response == BuyerOfferResponseType.Accepted);
        Assert.Contains(childResponses, r => r.BuyerId == buyerId2 && r.Response == BuyerOfferResponseType.Accepted);

        // Verify negotiation log
        var logs = await Context.NegotiationLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(childOffer.Id, logs[0].OfferId);
        Assert.Equal(50m, logs[0].PreviousPrice);
        Assert.Equal(45m, logs[0].NewPrice);

        // Verify Expiry Job is scheduled
        JobDispatcher.Received(1).Schedule(
            Arg.Any<Expression<Func<IGroupRequestOfferExpiryJob, Task>>>(),
            Arg.Any<DateTimeOffset>());

        // Verify notifications created
        var notifications = await Context.Notifications.ToListAsync();
        Assert.Equal(3, notifications.Count); // 2 buyers + 1 supplier
        Assert.Contains(notifications, n => n.UserId == buyerId1 && n.Type == NotificationType.GroupRequestOfferPlaced);
        Assert.Contains(notifications, n => n.UserId == supplierId && n.Type == NotificationType.OfferCountered);
    }
}
