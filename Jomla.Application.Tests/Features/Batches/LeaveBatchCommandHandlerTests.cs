using Jomla.Application.Features.Batches.Commands.LeaveBatch;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.Batches;

public class LeaveBatchCommandHandlerTests : ApplicationTestBase
{
    private readonly ILogger<LeaveBatchCommandHandler> _logger;
    private readonly LeaveBatchCommandHandler _handler;

    public LeaveBatchCommandHandlerTests()
    {
        _logger = Substitute.For<ILogger<LeaveBatchCommandHandler>>();
        _handler = new LeaveBatchCommandHandler(
            Context,
            StripePaymentService,
            _logger,
            Mediator);
    }

    [Fact]
    public async Task Handle_BatchNotFound_ReturnsError()
    {
        // Arrange
        var command = new LeaveBatchCommand
        {
            BatchId = Guid.NewGuid(),
            BuyerId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task Handle_BatchNotOpen_ReturnsError()
    {
        // Arrange
        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Product",
            Status = SupplierOfferStatus.Active
        };
        var batch = new SupplierBatch
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            Offer = offer,
            Status = BatchStatus.Completed
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        await Context.SaveChangesAsync();

        var command = new LeaveBatchCommand
        {
            BatchId = batch.Id,
            BuyerId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot leave a batch that is", result.Error);
    }

    [Fact]
    public async Task Handle_ParticipantNotActive_ReturnsError()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Product",
            Status = SupplierOfferStatus.Active
        };
        var batch = new SupplierBatch
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            Offer = offer,
            Status = BatchStatus.Open
        };
        // Participant is left
        var participant = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId,
            Status = BatchParticipantStatus.Left,
            Quantity = 2
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        Context.BatchParticipants.Add(participant);
        await Context.SaveChangesAsync();

        var command = new LeaveBatchCommand
        {
            BatchId = batch.Id,
            BuyerId = buyerId
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("You are not an active participant", result.Error);
    }

    [Fact]
    public async Task Handle_Success_UpdatesParticipantStatusToLeftDecrementsQuantityAndCancelsStripePayment()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Product",
            Status = SupplierOfferStatus.Active
        };
        var batch = new SupplierBatch
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            Offer = offer,
            Status = BatchStatus.Open,
            CurrentQuantity = 5
        };
        var participant = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId,
            Status = BatchParticipantStatus.Active,
            Quantity = 2,
            StripePaymentIntentId = "pi_hold_abc"
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        Context.BatchParticipants.Add(participant);
        await Context.SaveChangesAsync();

        StripePaymentService.CancelPaymentAsync("pi_hold_abc", Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult { Success = true });

        var command = new LeaveBatchCommand
        {
            BatchId = batch.Id,
            BuyerId = buyerId
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.RemainingQuantity); // 5 - 2

        var updatedParticipant = await Context.BatchParticipants
            .FirstOrDefaultAsync(p => p.BatchId == batch.Id && p.BuyerId == buyerId);
        Assert.NotNull(updatedParticipant);
        Assert.Equal(BatchParticipantStatus.Left, updatedParticipant.Status);

        // Stripe cancellation should be called
        await StripePaymentService.Received(1).CancelPaymentAsync("pi_hold_abc", Arg.Any<CancellationToken>());
    }
}
