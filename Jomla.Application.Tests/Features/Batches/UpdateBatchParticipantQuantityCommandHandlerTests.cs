using Jomla.Application.Features.Batches.Commands.UpdateBatch;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Fulfillment;
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

namespace Jomla.Application.Tests.Features.Batches;

public class UpdateBatchParticipantQuantityCommandHandlerTests : ApplicationTestBase
{
    private readonly UpdateBatchParticipantQuantityCommandHandler _handler;

    public UpdateBatchParticipantQuantityCommandHandlerTests()
    {
        _handler = new UpdateBatchParticipantQuantityCommandHandler(
            Context,
            StripePaymentService,
            JobDispatcher,
            Mediator);
    }

    [Fact]
    public async Task Handle_BatchNotFound_ReturnsError()
    {
        // Arrange
        var command = new UpdateBatchParticipantQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), "buyer@email.com", 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Batch not found", result.Error);
    }

    [Fact]
    public async Task Handle_BatchNotOpen_ReturnsError()
    {
        // Arrange
        var offer = new SupplierOffer { Id = Guid.NewGuid(), Title = "Product", Status = SupplierOfferStatus.Active };
        var batch = new SupplierBatch { Id = Guid.NewGuid(), OfferId = offer.Id, Offer = offer, Status = BatchStatus.Completed };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        await Context.SaveChangesAsync();

        var command = new UpdateBatchParticipantQuantityCommand(batch.Id, Guid.NewGuid(), "buyer@email.com", 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot edit quantity", result.Error);
    }

    [Fact]
    public async Task Handle_ParticipantNotActive_ReturnsError()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var offer = new SupplierOffer { Id = Guid.NewGuid(), Title = "Product", Status = SupplierOfferStatus.Active };
        var batch = new SupplierBatch { Id = Guid.NewGuid(), OfferId = offer.Id, Offer = offer, Status = BatchStatus.Open };
        // Participant has status Left
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

        var command = new UpdateBatchParticipantQuantityCommand(batch.Id, buyerId, "buyer@email.com", 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("You are not an active participant", result.Error);
    }

    [Fact]
    public async Task Handle_QuantityIdentical_ReturnsSuccessWithoutChanges()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var offer = new SupplierOffer { Id = Guid.NewGuid(), Title = "Product", Status = SupplierOfferStatus.Active };
        var batch = new SupplierBatch { Id = Guid.NewGuid(), OfferId = offer.Id, Offer = offer, Status = BatchStatus.Open };
        var participant = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId,
            Status = BatchParticipantStatus.Active,
            Quantity = 5
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        Context.BatchParticipants.Add(participant);
        await Context.SaveChangesAsync();

        var command = new UpdateBatchParticipantQuantityCommand(batch.Id, buyerId, "buyer@email.com", 5); // 5 == 5

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("No changes made. Quantity is identical.", result.Error);
        await StripePaymentService.DidNotReceiveWithAnyArgs().CreatePaymentHoldAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InsufficientSlots_ReturnsError()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var offer = new SupplierOffer { Id = Guid.NewGuid(), Title = "Product", Status = SupplierOfferStatus.Active };
        var batch = new SupplierBatch { Id = Guid.NewGuid(), OfferId = offer.Id, Offer = offer, Status = BatchStatus.Open, TargetQuantity = 10, CurrentQuantity = 8 };
        var participant = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId,
            Status = BatchParticipantStatus.Active,
            Quantity = 3
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        Context.BatchParticipants.Add(participant);
        await Context.SaveChangesAsync();

        var command = new UpdateBatchParticipantQuantityCommand(batch.Id, buyerId, "buyer@email.com", 6); // Delta = 6 - 3 = 3 > spaceRemaining (10 - 8 = 2)

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Insufficient slots", result.Error);
    }

    [Fact]
    public async Task Handle_StripePaymentHoldFails_ReturnsError()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var offer = new SupplierOffer { Id = Guid.NewGuid(), Title = "Product", UnitPrice = 100m, DiscountPercentage = 10m, Status = SupplierOfferStatus.Active };
        var batch = new SupplierBatch { Id = Guid.NewGuid(), OfferId = offer.Id, Offer = offer, Status = BatchStatus.Open, TargetQuantity = 10, CurrentQuantity = 5 };
        var participant = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId,
            Status = BatchParticipantStatus.Active,
            Quantity = 2,
            StripePaymentIntentId = "pi_old"
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        Context.BatchParticipants.Add(participant);
        await Context.SaveChangesAsync();

        StripePaymentService.CreatePaymentHoldAsync(
            buyerId.ToString(),
            "buyer@email.com",
            360m, // 4 * 100 * 0.9 = 360
            batch.Id,
            cancellationToken: Arg.Any<CancellationToken>()
        ).Returns(new StripePaymentIntentResult { Success = false, Error = "Failed authorization" });

        var command = new UpdateBatchParticipantQuantityCommand(batch.Id, buyerId, "buyer@email.com", 4);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Stripe payment hold failed", result.Error);
    }

    [Fact]
    public async Task Handle_SuccessNoCompletion_UpdatesQuantityAndCancelsOldStripePayment()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var offer = new SupplierOffer { Id = Guid.NewGuid(), Title = "Product", UnitPrice = 100m, DiscountPercentage = 10m, Status = SupplierOfferStatus.Active };
        var batch = new SupplierBatch { Id = Guid.NewGuid(), OfferId = offer.Id, Offer = offer, Status = BatchStatus.Open, TargetQuantity = 10, CurrentQuantity = 5 };
        var participant = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId,
            Status = BatchParticipantStatus.Active,
            Quantity = 2,
            StripePaymentIntentId = "pi_old"
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        Context.BatchParticipants.Add(participant);
        await Context.SaveChangesAsync();

        StripePaymentService.CreatePaymentHoldAsync(
            buyerId.ToString(),
            "buyer@email.com",
            360m,
            batch.Id,
            cancellationToken: Arg.Any<CancellationToken>()
        ).Returns(new StripePaymentIntentResult
        {
            Success = true,
            PaymentIntentId = "pi_new",
            ClientSecret = "secret_new"
        });

        var command = new UpdateBatchParticipantQuantityCommand(batch.Id, buyerId, "buyer@email.com", 4); // delta = 2

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4, result.UpdatedQuantity);
        Assert.Equal(360m, result.NewTotalAmount);
        Assert.Equal("pi_new", result.NewPaymentIntentId);

        var updatedBatch = await Context.SupplierBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(7, updatedBatch.CurrentQuantity); // 5 + 2

        var updatedParticipant = await Context.BatchParticipants
            .FirstOrDefaultAsync(p => p.BatchId == batch.Id && p.BuyerId == buyerId);
        Assert.NotNull(updatedParticipant);
        Assert.Equal(4, updatedParticipant.Quantity);
        Assert.Equal("pi_new", updatedParticipant.StripePaymentIntentId);

        // Verify old Stripe hold cancelled
        await StripePaymentService.Received(1).CancelPaymentAsync("pi_old", Arg.Any<CancellationToken>());

        // Batch completion should NOT be enqueued as 7 < 10
        JobDispatcher.DidNotReceiveWithAnyArgs().Enqueue(Arg.Any<Expression<Func<IBatchCompletionJob, Task>>>());
    }

    [Fact]
    public async Task Handle_SuccessWithCompletionTrigger_UpdatesQuantityCancelsOldPaymentAndEnqueuesFulfillmentJob()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var offer = new SupplierOffer { Id = Guid.NewGuid(), Title = "Product", UnitPrice = 100m, DiscountPercentage = 10m, Status = SupplierOfferStatus.Active };
        var batch = new SupplierBatch { Id = Guid.NewGuid(), OfferId = offer.Id, Offer = offer, Status = BatchStatus.Open, TargetQuantity = 10, CurrentQuantity = 8 };
        var participant = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId,
            Status = BatchParticipantStatus.Active,
            Quantity = 2,
            StripePaymentIntentId = "pi_old"
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        Context.BatchParticipants.Add(participant);
        await Context.SaveChangesAsync();

        StripePaymentService.CreatePaymentHoldAsync(
            buyerId.ToString(),
            "buyer@email.com",
            360m,
            batch.Id,
            cancellationToken: Arg.Any<CancellationToken>()
        ).Returns(new StripePaymentIntentResult
        {
            Success = true,
            PaymentIntentId = "pi_new",
            ClientSecret = "secret_new"
        });

        var command = new UpdateBatchParticipantQuantityCommand(batch.Id, buyerId, "buyer@email.com", 4); // delta = 2, new current = 10

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(10, result.BatchCurrentQuantity);

        // Verify old hold cancelled
        await StripePaymentService.Received(1).CancelPaymentAsync("pi_old", Arg.Any<CancellationToken>());

        // Verify BatchCompletionJob is enqueued since current (10) >= target (10)
        JobDispatcher.Received(1).Enqueue(Arg.Any<Expression<Func<IBatchCompletionJob, Task>>>());
    }
}
