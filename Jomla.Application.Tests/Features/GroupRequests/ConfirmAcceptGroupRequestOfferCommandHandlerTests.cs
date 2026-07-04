using Jomla.Application.Features.GroupRequests.Commands.AcceptGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Queries;
using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Fulfillment;
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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.GroupRequests;

public class ConfirmAcceptGroupRequestOfferCommandHandlerTests : ApplicationTestBase
{
    private readonly ILogger<ConfirmAcceptGroupRequestOfferCommandHandler> _logger;
    private readonly ConfirmAcceptGroupRequestOfferCommandHandler _handler;

    public ConfirmAcceptGroupRequestOfferCommandHandlerTests()
    {
        _logger = Substitute.For<ILogger<ConfirmAcceptGroupRequestOfferCommandHandler>>();
        _handler = new ConfirmAcceptGroupRequestOfferCommandHandler(
            Context,
            StripePaymentService,
            JobDispatcher,
            _logger,
            RealtimeService,
            Mediator);
    }

    [Fact]
    public async Task Handle_OfferNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new ConfirmAcceptGroupRequestOfferCommand(Guid.NewGuid(), Guid.NewGuid(), "buyer@email.com", 5, "pi_123");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OfferNotOpen_ThrowsConflictException()
    {
        // Arrange
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Status = GroupRequestStatus.Active };
        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            GroupRequest = groupRequest,
            Status = GroupRequestOfferStatus.Accepted
        };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestOffers.Add(offer);
        await Context.SaveChangesAsync();

        var command = new ConfirmAcceptGroupRequestOfferCommand(offer.Id, Guid.NewGuid(), "buyer@email.com", 5, "pi_123");

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GroupRequestNotActive_ThrowsConflictException()
    {
        // Arrange
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Status = GroupRequestStatus.Closed };
        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            GroupRequest = groupRequest,
            Status = GroupRequestOfferStatus.Open
        };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestOffers.Add(offer);
        await Context.SaveChangesAsync();

        var command = new ConfirmAcceptGroupRequestOfferCommand(offer.Id, Guid.NewGuid(), "buyer@email.com", 5, "pi_123");

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ParticipantNotActive_ThrowsForbiddenException()
    {
        // Arrange
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Status = GroupRequestStatus.Active };
        // Participant is left
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = Guid.NewGuid(),
            Status = GroupRequestParticipantStatus.Left
        };
        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            GroupRequest = groupRequest,
            Status = GroupRequestOfferStatus.Open
        };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        Context.GroupRequestOffers.Add(offer);
        await Context.SaveChangesAsync();

        var command = new ConfirmAcceptGroupRequestOfferCommand(offer.Id, participant.BuyerId, "buyer@email.com", 5, "pi_123");

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyAccepted_ThrowsConflictException()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Status = GroupRequestStatus.Active };
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId,
            Status = GroupRequestParticipantStatus.Active
        };
        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            GroupRequest = groupRequest,
            Status = GroupRequestOfferStatus.Open
        };
        var response = new BuyerOfferResponse
        {
            OfferId = offer.Id,
            BuyerId = buyerId,
            Response = BuyerOfferResponseType.Accepted
        };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        Context.GroupRequestOffers.Add(offer);
        Context.BuyerOfferResponses.Add(response);
        await Context.SaveChangesAsync();

        var command = new ConfirmAcceptGroupRequestOfferCommand(offer.Id, buyerId, "buyer@email.com", 5, "pi_123");

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AcceptedQuantityExceedsRemaining_CancelsPaymentAndThrowsConflictException()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Status = GroupRequestStatus.Active };
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId,
            Status = GroupRequestParticipantStatus.Active
        };
        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            GroupRequest = groupRequest,
            Status = GroupRequestOfferStatus.Open,
            QuantityAvailable = 5,
            AcceptedQuantity = 3
        };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        Context.GroupRequestOffers.Add(offer);
        await Context.SaveChangesAsync();

        StripePaymentService.CancelPaymentAsync("pi_123", Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult { Success = true });

        var command = new ConfirmAcceptGroupRequestOfferCommand(offer.Id, buyerId, "buyer@email.com", 3, "pi_123"); // 3 + 3 = 6 > 5

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
        await StripePaymentService.Received(1).CancelPaymentAsync("pi_123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StripeVerificationFails_ThrowsBadRequestException()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Status = GroupRequestStatus.Active };
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId,
            Status = GroupRequestParticipantStatus.Active
        };
        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            GroupRequest = groupRequest,
            Status = GroupRequestOfferStatus.Open,
            QuantityAvailable = 10,
            AcceptedQuantity = 0
        };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        Context.GroupRequestOffers.Add(offer);
        await Context.SaveChangesAsync();

        StripePaymentService.GetPaymentIntentAsync("pi_123", Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult { Success = false, Error = "Verify failed" });

        var command = new ConfirmAcceptGroupRequestOfferCommand(offer.Id, buyerId, "buyer@email.com", 5, "pi_123");

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_StripeVerificationMismatchAmount_CancelsPaymentAndThrowsBadRequestException()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Status = GroupRequestStatus.Active };
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId,
            Status = GroupRequestParticipantStatus.Active
        };
        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            GroupRequest = groupRequest,
            Status = GroupRequestOfferStatus.Open,
            QuantityAvailable = 10,
            AcceptedQuantity = 0,
            CurrentUnitPrice = 50m
        };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        Context.GroupRequestOffers.Add(offer);
        await Context.SaveChangesAsync();

        StripePaymentService.GetPaymentIntentAsync("pi_123", Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult
            {
                Success = true,
                Status = "requires_capture",
                Amount = 10000 // 100 dollars in cents (Expected is 5 * 50 = 250 dollars = 25000 cents)
            });

        var command = new ConfirmAcceptGroupRequestOfferCommand(offer.Id, buyerId, "buyer@email.com", 5, "pi_123");

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _handler.Handle(command, CancellationToken.None));
        await StripePaymentService.Received(1).CancelPaymentAsync("pi_123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesBuyerOfferResponseAndIncrementsAcceptedQuantity()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Status = GroupRequestStatus.Active };
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId,
            Status = GroupRequestParticipantStatus.Active
        };
        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            GroupRequest = groupRequest,
            Status = GroupRequestOfferStatus.Open,
            QuantityAvailable = 10,
            AcceptedQuantity = 0,
            CurrentUnitPrice = 50m
        };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        Context.GroupRequestOffers.Add(offer);
        await Context.SaveChangesAsync();

        StripePaymentService.GetPaymentIntentAsync("pi_123", Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult
            {
                Success = true,
                Status = "requires_capture",
                Amount = 25000 // 250 dollars in cents
            });

        var command = new ConfirmAcceptGroupRequestOfferCommand(offer.Id, buyerId, "buyer@email.com", 5, "pi_123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);

        var updatedOffer = await Context.GroupRequestOffers.FindAsync(offer.Id);
        Assert.NotNull(updatedOffer);
        Assert.Equal(5, updatedOffer.AcceptedQuantity);

        var response = await Context.BuyerOfferResponses
            .FirstOrDefaultAsync(r => r.OfferId == offer.Id && r.BuyerId == buyerId);
        Assert.NotNull(response);
        Assert.Equal(BuyerOfferResponseType.Accepted, response.Response);
        Assert.Equal(5, response.AcceptedQuantity);
        Assert.Equal("pi_123", response.StripePaymentIntentId);

        // Fill job should not be enqueued as 5 < 10 (not fully accepted yet)
        JobDispatcher.DidNotReceiveWithAnyArgs().Enqueue(Arg.Any<Expression<Func<IGroupRequestOfferFillJob, Task>>>());
    }

    [Fact]
    public async Task Handle_OfferFullyAccepted_EnqueuesFulfillmentJob()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var groupRequest = new GroupRequest { Id = Guid.NewGuid(), Status = GroupRequestStatus.Active };
        var participant = new GroupRequestParticipant
        {
            GroupRequestId = groupRequest.Id,
            BuyerId = buyerId,
            Status = GroupRequestParticipantStatus.Active
        };
        var offer = new GroupRequestOffer
        {
            Id = Guid.NewGuid(),
            GroupRequestId = groupRequest.Id,
            GroupRequest = groupRequest,
            Status = GroupRequestOfferStatus.Open,
            QuantityAvailable = 10,
            AcceptedQuantity = 5,
            CurrentUnitPrice = 50m
        };
        Context.GroupRequests.Add(groupRequest);
        Context.GroupRequestParticipants.Add(participant);
        Context.GroupRequestOffers.Add(offer);
        await Context.SaveChangesAsync();

        StripePaymentService.GetPaymentIntentAsync("pi_123", Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult
            {
                Success = true,
                Status = "requires_capture",
                Amount = 25000 // 250 dollars in cents (5 * 50)
            });

        var command = new ConfirmAcceptGroupRequestOfferCommand(offer.Id, buyerId, "buyer@email.com", 5, "pi_123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);

        var updatedOffer = await Context.GroupRequestOffers.FindAsync(offer.Id);
        Assert.NotNull(updatedOffer);
        Assert.Equal(10, updatedOffer.AcceptedQuantity); // 5 + 5 = 10 (fully filled)

        // Fill job should be enqueued
        JobDispatcher.Received(1).Enqueue(Arg.Any<Expression<Func<IGroupRequestOfferFillJob, Task>>>());
    }
}
