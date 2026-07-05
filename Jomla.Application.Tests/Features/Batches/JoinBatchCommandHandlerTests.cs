using Jomla.Application.Features.Batches.Commands.JoinBatch;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.Batches;

public class JoinBatchCommandHandlerTests : ApplicationTestBase
{
    private readonly JoinBatchCommandHandler _handler;

    public JoinBatchCommandHandlerTests()
    {
        _handler = new JoinBatchCommandHandler(
            Context,
            StripePaymentService,
            JobDispatcher);
    }

    [Fact]
    public async Task Handle_BatchNotFound_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = new JoinBatchCommand
        {
            BatchId = Guid.NewGuid(),
            BuyerId = Guid.NewGuid(),
            BuyerEmail = "buyer@jomla.com",
            Quantity = 2
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Handle_BatchNotOpen_ReturnsInvalidBatchStatusResponse()
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

        var command = new JoinBatchCommand
        {
            BatchId = batch.Id,
            BuyerId = Guid.NewGuid(),
            BuyerEmail = "buyer@jomla.com",
            Quantity = 2
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_BATCH_STATUS", result.ErrorCode);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Handle_AlreadyActiveParticipant_ReturnsAlreadyParticipantResponse()
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
        var participant = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId,
            Status = BatchParticipantStatus.Active,
            Quantity = 1
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        Context.BatchParticipants.Add(participant);
        await Context.SaveChangesAsync();

        var command = new JoinBatchCommand
        {
            BatchId = batch.Id,
            BuyerId = buyerId,
            BuyerEmail = "buyer@jomla.com",
            Quantity = 2
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ALREADY_PARTICIPANT", result.ErrorCode);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Handle_InsufficientSlots_ReturnsInsufficientSlotsResponse()
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
            Status = BatchStatus.Open,
            TargetQuantity = 10,
            CurrentQuantity = 8
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        await Context.SaveChangesAsync();

        var command = new JoinBatchCommand
        {
            BatchId = batch.Id,
            BuyerId = Guid.NewGuid(),
            BuyerEmail = "buyer@jomla.com",
            Quantity = 3 // 8 + 3 = 11 > 10
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INSUFFICIENT_SLOTS", result.ErrorCode);
        Assert.Equal(2, result.SlotsAvailable);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Handle_PaymentHoldFails_ReturnsPaymentHoldFailedResponse()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Product",
            UnitPrice = 100m,
            DiscountPercentage = 10m,
            Status = SupplierOfferStatus.Active
        };
        var batch = new SupplierBatch
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            Offer = offer,
            Status = BatchStatus.Open,
            TargetQuantity = 10,
            CurrentQuantity = 5
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        await Context.SaveChangesAsync();

        StripePaymentService.CreatePaymentHoldAsync(
            buyerId.ToString(),
            "buyer@jomla.com",
            180m, // 2 * 100 * 0.9 = 180
            batch.Id,
            cancellationToken: Arg.Any<CancellationToken>()
        ).Returns(new StripePaymentIntentResult { Success = false, Error = "Card declined", ErrorCode = "card_declined" });

        var command = new JoinBatchCommand
        {
            BatchId = batch.Id,
            BuyerId = buyerId,
            BuyerEmail = "buyer@jomla.com",
            Quantity = 2
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("card_declined", result.ErrorCode);
        Assert.Equal("Payment hold failed: Card declined", result.Error);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Handle_SuccessfulJoin_ReturnsSuccessWithHoldDetails()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Product",
            UnitPrice = 100m,
            DiscountPercentage = 10m,
            Status = SupplierOfferStatus.Active
        };
        var batch = new SupplierBatch
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            Offer = offer,
            Status = BatchStatus.Open,
            TargetQuantity = 10,
            CurrentQuantity = 5
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        await Context.SaveChangesAsync();

        StripePaymentService.CreatePaymentHoldAsync(
            buyerId.ToString(),
            "buyer@jomla.com",
            180m,
            batch.Id,
            cancellationToken: Arg.Any<CancellationToken>()
        ).Returns(new StripePaymentIntentResult
        {
            Success = true,
            PaymentIntentId = "pi_hold_123",
            ClientSecret = "secret_123"
        });

        var command = new JoinBatchCommand
        {
            BatchId = batch.Id,
            BuyerId = buyerId,
            BuyerEmail = "buyer@jomla.com",
            Quantity = 2
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("pi_hold_123", result.PaymentIntentId);
        Assert.Equal("secret_123", result.ClientSecret);
        Assert.Equal(180m, result.TotalAmount);
    }
}
