using Jomla.Application.Features.Offers.Commands.ModerateSupplierOffer;
using Jomla.Application.Features.Batches.Commands.CreateBatch;
using Jomla.Application.Features.Notifications;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.Expiry;
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

namespace Jomla.Application.Tests.Features.Offers;

public class ModerateSupplierOfferCommandHandlerTests : ApplicationTestBase
{
    private readonly IModerationAgent _moderationAgent;
    private readonly ModerateSupplierOfferCommandHandler _handler;

    public ModerateSupplierOfferCommandHandlerTests()
    {
        _moderationAgent = Substitute.For<IModerationAgent>();
        _handler = new ModerateSupplierOfferCommandHandler(
            Context,
            _moderationAgent,
            Mediator,
            JobDispatcher,
            RealtimeService);
    }

    [Fact]
    public async Task Handle_OfferNotFound_ReturnsSilently()
    {
        // Arrange
        var command = new ModerateSupplierOfferCommand(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<CreateBatchCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyApprovedNoBatch_CreatesFirstBatch()
    {
        // Arrange
        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Approved Offer",
            SupplierId = Guid.NewGuid(),
            Status = SupplierOfferStatus.Active,
            ModerationStatus = ModerationStatus.Approved
        };
        Context.SupplierOffers.Add(offer);
        await Context.SaveChangesAsync();

        var command = new ModerateSupplierOfferCommand(offer.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Mediator.Received(1).Send(Arg.Is<CreateBatchCommand>(c => c.OfferId == offer.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyApprovedWithBatch_ReturnsSilently()
    {
        // Arrange
        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Approved Offer",
            SupplierId = Guid.NewGuid(),
            Status = SupplierOfferStatus.Active,
            ModerationStatus = ModerationStatus.Approved
        };
        var batch = new SupplierBatch
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            Offer = offer,
            Status = BatchStatus.Open
        };
        Context.SupplierOffers.Add(offer);
        Context.SupplierBatches.Add(batch);
        await Context.SaveChangesAsync();

        var command = new ModerateSupplierOfferCommand(offer.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<CreateBatchCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ModerationApproved_SetsStatusActiveAndSchedulesExpiryAndOpensFirstBatch()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(10);
        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Fresh Tomatoes",
            SupplierId = supplierId,
            ExpiresAt = expiresAt,
            Status = SupplierOfferStatus.PendingReview,
            ModerationStatus = ModerationStatus.Pending,
            ImageUrls = "[\"https://storage.jomla/img.jpg\"]"
        };
        Context.SupplierOffers.Add(offer);
        await Context.SaveChangesAsync();

        _moderationAgent.ModerateAsync(Arg.Any<ModerationInput>(), Arg.Any<CancellationToken>())
            .Returns(new ModerationResult(true, "Looks good"));

        JobDispatcher.Schedule(Arg.Any<Expression<Func<ISupplierOfferExpiryJob, Task>>>(), Arg.Any<DateTimeOffset>())
            .Returns("job_exp_123");

        var command = new ModerateSupplierOfferCommand(offer.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedOffer = await Context.SupplierOffers.FindAsync(offer.Id);
        Assert.NotNull(updatedOffer);
        Assert.Equal(SupplierOfferStatus.Active, updatedOffer.Status);
        Assert.Equal(ModerationStatus.Approved, updatedOffer.ModerationStatus);
        Assert.Equal("job_exp_123", updatedOffer.JobId);

        // Verify Expiry Job is scheduled
        JobDispatcher.Received(1).Schedule(
            Arg.Any<Expression<Func<ISupplierOfferExpiryJob, Task>>>(),
            Arg.Is<DateTimeOffset>(d => d.UtcDateTime == expiresAt));

        // Verify notification was created
        var notifications = await Context.Notifications.ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(supplierId, notifications[0].UserId);
        Assert.Equal(NotificationType.OfferApproved, notifications[0].Type);

        // Verify Realtime notifications sent
        await Mediator.Received(1).Send(Arg.Is<CreateBatchCommand>(c => c.OfferId == offer.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ModerationFlagged_SetsStatusInactiveAndDispatchesFlaggedNotification()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var offer = new SupplierOffer
        {
            Id = Guid.NewGuid(),
            Title = "Offensive Product Name",
            SupplierId = supplierId,
            Status = SupplierOfferStatus.PendingReview,
            ModerationStatus = ModerationStatus.Pending
        };
        Context.SupplierOffers.Add(offer);
        await Context.SaveChangesAsync();

        _moderationAgent.ModerateAsync(Arg.Any<ModerationInput>(), Arg.Any<CancellationToken>())
            .Returns(new ModerationResult(false, "Offensive words used"));

        var command = new ModerateSupplierOfferCommand(offer.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedOffer = await Context.SupplierOffers.FindAsync(offer.Id);
        Assert.NotNull(updatedOffer);
        Assert.Equal(SupplierOfferStatus.Inactive, updatedOffer.Status);
        Assert.Equal(ModerationStatus.Flagged, updatedOffer.ModerationStatus);
        Assert.Equal("Offensive words used", updatedOffer.ModerationReason);

        // No batch should be opened
        await Mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<CreateBatchCommand>(), Arg.Any<CancellationToken>());

        // Verify notification
        var notifications = await Context.Notifications.ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(supplierId, notifications[0].UserId);
        Assert.Equal(NotificationType.OfferFlagged, notifications[0].Type);
        Assert.Contains("Offensive words used", notifications[0].Body);
    }
}
