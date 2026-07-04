using Jomla.Application.Features.Batches.Commands.CompleteBatch;
using Jomla.Application.Features.Batches.Commands.OpenBatch;
using Jomla.Application.Features.Batches.Events;
using Jomla.Application.Features.Notifications;
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

namespace Jomla.Application.Tests.Features.Batches;

public class CompleteBatchCommandHandlerTests : ApplicationTestBase
{
    private readonly ILogger<CompleteBatchCommandHandler> _logger;
    private readonly CompleteBatchCommandHandler _handler;

    public CompleteBatchCommandHandlerTests()
    {
        _logger = Substitute.For<ILogger<CompleteBatchCommandHandler>>();
        _handler = new CompleteBatchCommandHandler(
            Context,
            StripePaymentService,
            Mediator,
            _logger,
            RealtimeService);
    }

    [Fact]
    public async Task Handle_BatchNotFound_ReturnsSilently()
    {
        // Arrange
        var command = new CompleteBatchCommand(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Empty(Context.Orders);
        await Mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<OpenBatchCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BatchAlreadyCompletedNoFailedOrders_ReturnsSilently()
    {
        // Arrange
        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Test Offer",
            SupplierId = Guid.NewGuid(),
            Status = SupplierOfferStatus.Active
        };
        var batch = new SupplierBatch
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            Offer = offer,
            Status = BatchStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };
        
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        await Context.SaveChangesAsync();

        var command = new CompleteBatchCommand(batch.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Empty(Context.Orders);
        await Mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<OpenBatchCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_CapturesPaymentsAndCompletesBatch()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var buyerId1 = Guid.NewGuid();
        var buyerId2 = Guid.NewGuid();

        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Awesome Product",
            SupplierId = supplierId,
            UnitPrice = 100m,
            DiscountPercentage = 10m,
            TotalQuantityAvailable = 50,
            Status = SupplierOfferStatus.Active
        };

        var batch = new SupplierBatch
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            Offer = offer,
            Status = BatchStatus.Open,
            TargetQuantity = 10,
            CurrentQuantity = 10
        };

        var p1 = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId1,
            Quantity = 4,
            StripePaymentIntentId = "pi_1",
            Status = BatchParticipantStatus.Active
        };

        var p2 = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId2,
            Quantity = 6,
            StripePaymentIntentId = "pi_2",
            Status = BatchParticipantStatus.Active
        };

        batch.Participants.Add(p1);
        batch.Participants.Add(p2);

        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        await Context.SaveChangesAsync();

        StripePaymentService.CapturePaymentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Jomla.Application.Common.Interfaces.StripePaymentIntentResult { Success = true });

        var command = new CompleteBatchCommand(batch.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Check batch updated
        var updatedBatch = await Context.SupplierBatches.FindAsync(batch.Id);
        Assert.NotNull(updatedBatch);
        Assert.Equal(BatchStatus.Completed, updatedBatch.Status);
        Assert.NotNull(updatedBatch.CompletedAt);

        // Check offer quantity decremented
        var updatedOffer = await Context.SupplierOffers.FindAsync(offer.Id);
        Assert.NotNull(updatedOffer);
        Assert.Equal(40, updatedOffer.TotalQuantityAvailable); // 50 - 10

        // Check orders created
        var orders = await Context.Orders.ToListAsync();
        Assert.Equal(2, orders.Count);
        Assert.Contains(orders, o => o.BuyerId == buyerId1 && o.Quantity == 4 && o.Status == OrderStatus.Paid);
        Assert.Contains(orders, o => o.BuyerId == buyerId2 && o.Quantity == 6 && o.Status == OrderStatus.Paid);

        // Verify Mediator sent OpenBatchCommand
        await Mediator.Received(1).Send(Arg.Is<OpenBatchCommand>(c => c.OfferId == offer.Id), Arg.Any<CancellationToken>());

        // Verify notifications created
        var notifications = await Context.Notifications.ToListAsync();
        Assert.Equal(3, notifications.Count); // 2 buyers + 1 supplier
        Assert.Contains(notifications, n => n.UserId == buyerId1 && n.Type == NotificationType.BatchCompleted);
        Assert.Contains(notifications, n => n.UserId == supplierId && n.Type == NotificationType.BatchCompleted);

        // Verify real-time status sends
        await RealtimeService.Received(1).SendUserBatchStatusChangedAsync(buyerId1, batch.Id, "Completed");
        await RealtimeService.Received(1).SendUserBatchStatusChangedAsync(buyerId2, batch.Id, "Completed");
    }

    [Fact]
    public async Task Handle_FailedCaptures_ThrowsExceptionAndStillOpensNextBatch()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var buyerId1 = Guid.NewGuid();
        var buyerId2 = Guid.NewGuid();

        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Awesome Product",
            SupplierId = supplierId,
            UnitPrice = 100m,
            DiscountPercentage = 10m,
            TotalQuantityAvailable = 50,
            Status = SupplierOfferStatus.Active
        };

        var batch = new SupplierBatch
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            Offer = offer,
            Status = BatchStatus.Open,
            TargetQuantity = 10,
            CurrentQuantity = 10
        };

        var p1 = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId1,
            Quantity = 4,
            StripePaymentIntentId = "pi_1",
            Status = BatchParticipantStatus.Active
        };

        var p2 = new BatchParticipant
        {
            BatchId = batch.Id,
            BuyerId = buyerId2,
            Quantity = 6,
            StripePaymentIntentId = "pi_2",
            Status = BatchParticipantStatus.Active
        };

        batch.Participants.Add(p1);
        batch.Participants.Add(p2);

        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        await Context.SaveChangesAsync();

        // P1 succeeds, P2 fails
        StripePaymentService.CapturePaymentAsync("pi_1", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Jomla.Application.Common.Interfaces.StripePaymentIntentResult { Success = true });
        StripePaymentService.CapturePaymentAsync("pi_2", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Jomla.Application.Common.Interfaces.StripePaymentIntentResult { Success = false });

        var command = new CompleteBatchCommand(batch.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("One or more captures failed", exception.Message);

        // Verify orders created and statuses
        var orders = await Context.Orders.ToListAsync();
        Assert.Equal(2, orders.Count);
        Assert.Contains(orders, o => o.BuyerId == buyerId1 && o.Status == OrderStatus.Paid);
        Assert.Contains(orders, o => o.BuyerId == buyerId2 && o.Status == OrderStatus.Failed);

        // OpenNextBatch should still be called
        await Mediator.Received(1).Send(Arg.Is<OpenBatchCommand>(c => c.OfferId == offer.Id), Arg.Any<CancellationToken>());
    }
}
