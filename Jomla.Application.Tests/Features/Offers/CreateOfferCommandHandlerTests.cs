using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Offers.Commands.CreateOffer;
using Jomla.Application.Jobs.Agents;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.Offers;

public class CreateOfferCommandHandlerTests : ApplicationTestBase
{
    private readonly IImageService _imageService;
    private readonly CreateOfferCommandHandler _handler;

    public CreateOfferCommandHandlerTests()
    {
        _imageService = Substitute.For<IImageService>();
        _handler = new CreateOfferCommandHandler(
            Context,
            IdentityService,
            _imageService,
            JobDispatcher);
    }

    [Fact]
    public async Task Handle_UserNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        IdentityService.GetCurrentUserId().Returns(Guid.Empty);
        var command = new CreateOfferCommand(
            "Title", "Description", Guid.NewGuid(), 100m, 10m, 5, 20, null, null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithImages_UploadsImagesAndCreatesOfferPendingReview()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        IdentityService.GetCurrentUserId().Returns(supplierId);

        var mockFile1 = Substitute.For<IFormFile>();
        mockFile1.FileName.Returns("img1.jpg");
        var mockFile2 = Substitute.For<IFormFile>();
        mockFile2.FileName.Returns("img2.png");

        _imageService.UploadImageAsync(mockFile1, Arg.Any<CancellationToken>())
            .Returns("https://jomla.storage/img1.jpg");
        _imageService.UploadImageAsync(mockFile2, Arg.Any<CancellationToken>())
            .Returns("https://jomla.storage/img2.png");

        var command = new CreateOfferCommand(
            Title: "Fresh Tomato Batch",
            Description: "Fresh organic tomatoes",
            CategoryId: Guid.NewGuid(),
            UnitPrice: 12.5m,
            DiscountPercentage: 5m,
            BatchTargetQuantity: 10,
            TotalQuantityAvailable: 100,
            MinFallbackQuantity: 8,
            VariantAttributes: "{\"color\":\"red\"}",
            ExpiresAt: DateTime.UtcNow.AddDays(7),
            Images: [mockFile1, mockFile2]
        );

        // Act
        var resultOfferId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultOfferId);

        var savedOffer = await Context.SupplierOffers.FindAsync(resultOfferId);
        Assert.NotNull(savedOffer);
        Assert.Equal(supplierId, savedOffer.SupplierId);
        Assert.Equal(command.Title, savedOffer.Title);
        Assert.Equal(command.Description, savedOffer.Description);
        Assert.Equal(command.UnitPrice, savedOffer.UnitPrice);
        Assert.Equal(command.DiscountPercentage, savedOffer.DiscountPercentage);
        Assert.Equal(command.BatchTargetQuantity, savedOffer.BatchTargetQuantity);
        Assert.Equal(command.TotalQuantityAvailable, savedOffer.TotalQuantityAvailable);
        Assert.Equal(command.MinFallbackQuantity, savedOffer.MinFallbackQuantity);
        Assert.Equal(command.VariantAttributes, savedOffer.VariantAttributes);
        Assert.Equal(command.ExpiresAt, savedOffer.ExpiresAt);
        Assert.Equal(SupplierOfferStatus.PendingReview, savedOffer.Status);
        Assert.Equal(ModerationStatus.Pending, savedOffer.ModerationStatus);

        // Verify image URLs are uploaded and JSON serialized
        Assert.NotNull(savedOffer.ImageUrls);
        var urls = JsonSerializer.Deserialize<List<string>>(savedOffer.ImageUrls);
        Assert.NotNull(urls);
        Assert.Equal(2, urls.Count);
        Assert.Equal("https://jomla.storage/img1.jpg", urls[0]);
        Assert.Equal("https://jomla.storage/img2.png", urls[1]);

        // Verify Moderation Job is enqueued
        JobDispatcher.Received(1).Enqueue(Arg.Any<Expression<Func<IModerateSupplierOfferJob, Task>>>());
    }
}
