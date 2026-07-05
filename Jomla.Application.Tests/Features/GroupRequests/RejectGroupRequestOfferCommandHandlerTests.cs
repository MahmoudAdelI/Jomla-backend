using Jomla.Application.Features.GroupRequests.Commands.RejectGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.NegotiateGroupRequestOffer;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.GroupRequests;

public class RejectGroupRequestOfferCommandHandlerTests : ApplicationTestBase
{
    private readonly ILogger<RejectGroupRequestOfferCommandHandler> _logger;
    private readonly RejectGroupRequestOfferCommandHandler _handler;

    public RejectGroupRequestOfferCommandHandlerTests()
    {
        _logger = Substitute.For<ILogger<RejectGroupRequestOfferCommandHandler>>();
        _handler = new RejectGroupRequestOfferCommandHandler(
            Context,
            Mediator,
            _logger,
            RealtimeService);
    }

    [Fact]
    public async Task Handle_OfferNotFound_ReturnsError()
    {
        // Arrange
        var command = new RejectGroupRequestOfferCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Offer not found.", result.Error);
    }

    [Fact]
    public async Task Handle_OfferNotOpen_ReturnsError()
    {
        // Arrange
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Title = "Request", Category = new Category { Name = "Grains" } };
        var offer = new GroupRequestOffer { Id = Guid.NewGuid(), GroupRequestId = groupRequest.Id, GroupRequest = groupRequest, Status = GroupRequestOfferStatus.Countered };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestOffers.Add(offer);
        await Context.SaveChangesAsync();

        var command = new RejectGroupRequestOfferCommand(offer.Id, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal($"Offer is already {offer.Status}.", result.Error);
    }

    [Fact]
    public async Task Handle_BuyerNotActiveParticipant_ReturnsError()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Title = "Request", Category = new Category { Name = "Grains" } };
        // Participant has status Left
        var participant = new GroupRequestParticipant { GroupRequestId = groupRequest.Id, BuyerId = buyerId, Status = GroupRequestParticipantStatus.Left };
        var offer = new GroupRequestOffer { Id = Guid.NewGuid(), GroupRequestId = groupRequest.Id, GroupRequest = groupRequest, Status = GroupRequestOfferStatus.Open };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        Context.GroupRequestOffers.Add(offer);
        await Context.SaveChangesAsync();

        var command = new RejectGroupRequestOfferCommand(offer.Id, buyerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You are not an active participant in this group request.", result.Error);
    }

    [Fact]
    public async Task Handle_BuyerHasAcceptedResponse_ReturnsError()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Title = "Request", Category = new Category { Name = "Grains" } };
        var participant = new GroupRequestParticipant { GroupRequestId = groupRequest.Id, BuyerId = buyerId, Status = GroupRequestParticipantStatus.Active };
        var offer = new GroupRequestOffer { Id = Guid.NewGuid(), GroupRequestId = groupRequest.Id, GroupRequest = groupRequest, Status = GroupRequestOfferStatus.Open };
        var existingResponse = new BuyerOfferResponse { OfferId = offer.Id, Offer = offer, BuyerId = buyerId, Response = BuyerOfferResponseType.Accepted };

        offer.Responses.Add(existingResponse);
        groupRequest.Participants.Add(participant);

        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestOffers.Add(offer);
        Context.BuyerOfferResponses.Add(existingResponse);
        await Context.SaveChangesAsync();

        var command = new RejectGroupRequestOfferCommand(offer.Id, buyerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You must cancel your acceptance before you can reject the offer.", result.Error);
    }

    [Fact]
    public async Task Handle_AlreadyRejected_ReturnsSuccessNoChanges()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Title = "Request", Category = new Category { Name = "Grains" } };
        var participant = new GroupRequestParticipant { GroupRequestId = groupRequest.Id, BuyerId = buyerId, Status = GroupRequestParticipantStatus.Active };
        var offer = new GroupRequestOffer { Id = Guid.NewGuid(), GroupRequestId = groupRequest.Id, GroupRequest = groupRequest, Status = GroupRequestOfferStatus.Open };
        var existingResponse = new BuyerOfferResponse { OfferId = offer.Id, Offer = offer, BuyerId = buyerId, Response = BuyerOfferResponseType.Rejected };

        offer.Responses.Add(existingResponse);
        groupRequest.Participants.Add(participant);

        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestOffers.Add(offer);
        Context.BuyerOfferResponses.Add(existingResponse);
        await Context.SaveChangesAsync();

        var command = new RejectGroupRequestOfferCommand(offer.Id, buyerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        // Ensure no extra response was created
        var responseCount = await Context.BuyerOfferResponses.CountAsync();
        Assert.Equal(1, responseCount);
    }

    [Fact]
    public async Task Handle_FirstRejection_SavesResponseAndDoesNotTriggerNegotiation()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Title = "Request", Category = new Category { Name = "Grains" } };
        var participant = new GroupRequestParticipant { GroupRequestId = groupRequest.Id, BuyerId = buyerId, Status = GroupRequestParticipantStatus.Active };
        var offer = new GroupRequestOffer { Id = Guid.NewGuid(), GroupRequestId = groupRequest.Id, GroupRequest = groupRequest, Status = GroupRequestOfferStatus.Open };

        groupRequest.Participants.Add(participant);

        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestOffers.Add(offer);
        await Context.SaveChangesAsync();

        var command = new RejectGroupRequestOfferCommand(offer.Id, buyerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        var savedResponse = await Context.BuyerOfferResponses.FirstOrDefaultAsync(r => r.OfferId == offer.Id && r.BuyerId == buyerId);
        Assert.NotNull(savedResponse);
        Assert.Equal(BuyerOfferResponseType.Rejected, savedResponse.Response);

        // Should NOT trigger negotiation (only 1 rejection < 3)
        await Mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<NegotiateGroupRequestOfferCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThirdRejection_SavesResponseAndTriggersNegotiation()
    {
        // Arrange
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();
        var b3 = Guid.NewGuid();

        var category = new Category { Name = "Grains" };
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Title = "Request", Category = category };
        var p1 = new GroupRequestParticipant { GroupRequestId = groupRequest.Id, BuyerId = b1, Status = GroupRequestParticipantStatus.Active };
        var p2 = new GroupRequestParticipant { GroupRequestId = groupRequest.Id, BuyerId = b2, Status = GroupRequestParticipantStatus.Active };
        var p3 = new GroupRequestParticipant { GroupRequestId = groupRequest.Id, BuyerId = b3, Status = GroupRequestParticipantStatus.Active };

        var offer = new GroupRequestOffer { Id = Guid.NewGuid(), GroupRequestId = groupRequest.Id, GroupRequest = groupRequest, Status = GroupRequestOfferStatus.Open };

        var r1 = new BuyerOfferResponse { OfferId = offer.Id, Offer = offer, BuyerId = b1, Response = BuyerOfferResponseType.Rejected };
        var r2 = new BuyerOfferResponse { OfferId = offer.Id, Offer = offer, BuyerId = b2, Response = BuyerOfferResponseType.Rejected };

        offer.Responses.Add(r1);
        offer.Responses.Add(r2);
        groupRequest.Participants.Add(p1);
        groupRequest.Participants.Add(p2);
        groupRequest.Participants.Add(p3);

        Context.Categories.Add(category);
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestOffers.Add(offer);
        Context.BuyerOfferResponses.AddRange(r1, r2);
        await Context.SaveChangesAsync();

        var command = new RejectGroupRequestOfferCommand(offer.Id, b3); // This makes 3 rejections total

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        var savedResponse3 = await Context.BuyerOfferResponses.FirstOrDefaultAsync(r => r.OfferId == offer.Id && r.BuyerId == b3);
        Assert.NotNull(savedResponse3);
        Assert.Equal(BuyerOfferResponseType.Rejected, savedResponse3.Response);

        // Verification of Negotiation trigger (due to 3+ rejections)
        await Mediator.Received(1).Send(
            Arg.Is<NegotiateGroupRequestOfferCommand>(c => c.OfferId == offer.Id && c.categoryName == "Grains"),
            Arg.Any<CancellationToken>());
    }
}
