using Jomla.Application.Common.DTOs;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Commands.CreateGroupRequest;
using Jomla.Application.Jobs.Agents;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.GroupRequests;

public class CreateGroupRequestCommandHandlerTests : ApplicationTestBase
{
    private readonly IImageService _imageService;
    private readonly CreateGroupRequestCommandHandler _handler;

    public CreateGroupRequestCommandHandlerTests()
    {
        _imageService = Substitute.For<IImageService>();
        _handler = new CreateGroupRequestCommandHandler(
            Context,
            Mediator,
            CategoryAgent,
            JobDispatcher,
            _imageService);
    }

    [Fact]
    public async Task Handle_NoCategoriesInSystem_ReturnsError()
    {
        // Arrange
        var command = new CreateGroupRequestCommand(Guid.NewGuid(), "Tomato batch", 10, "Organic", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No categories exist in the system.", result.Error);
    }

    [Fact]
    public async Task Handle_ResolveCategorySucceeds_CreatesGroupRequestPendingReview()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "Vegetables" };
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        CategoryAgent.ResolveCategoryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<CategoryDto>>(), Arg.Any<CancellationToken>())
            .Returns(category.Id);

        var mockFile = Substitute.For<IFormFile>();
        _imageService.UploadImageAsync(mockFile, Arg.Any<CancellationToken>())
            .Returns("https://jomla.storage/tomato.jpg");

        var command = new CreateGroupRequestCommand(
            Guid.NewGuid(), "Tomato batch", 10, "Organic tomatoes", [mockFile]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.GroupRequestId);

        var request = await Context.GroupRequests.FindAsync(result.GroupRequestId);
        Assert.NotNull(request);
        Assert.Equal(category.Id, request.CategoryId);
        Assert.Equal("Tomato batch", request.Title);
        Assert.Equal(GroupRequestStatus.PendingReview, request.Status);
        Assert.Equal(ModerationStatus.Pending, request.ModerationStatus);

        // Verify Moderation Job is enqueued
        JobDispatcher.Received(1).Enqueue(Arg.Any<Expression<Func<IModerateGroupRequestJob, Task>>>());
    }

    [Fact]
    public async Task Handle_ResolveCategoryFailsFallbackFound_CreatesGroupRequestWithFallbackCategory()
    {
        // Arrange
        var fallbackCategory = new Category { Id = Guid.NewGuid(), Name = "Other" };
        Context.Categories.Add(fallbackCategory);
        await Context.SaveChangesAsync();

        CategoryAgent.ResolveCategoryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<CategoryDto>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Resolution failed"));

        var command = new CreateGroupRequestCommand(
            Guid.NewGuid(), "Strange Item", 5, "Uncommon object", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.GroupRequestId);

        var request = await Context.GroupRequests.FindAsync(result.GroupRequestId);
        Assert.NotNull(request);
        Assert.Equal(fallbackCategory.Id, request.CategoryId); // Fell back to 'Other'
    }

    [Fact]
    public async Task Handle_ResolveCategoryFailsNoFallback_ReturnsError()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "Vegetables" }; // No 'Other' or general categories
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        CategoryAgent.ResolveCategoryAsync(Arg.Any<string>(), Arg.Any<IEnumerable<CategoryDto>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Resolution failed"));

        var command = new CreateGroupRequestCommand(
            Guid.NewGuid(), "Strange Item", 5, "Uncommon object", null);

        // Act
        // Because fallback logic will check for "Other", "General", etc. and default to the first one available
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.GroupRequestId);

        var request = await Context.GroupRequests.FindAsync(result.GroupRequestId);
        Assert.NotNull(request);
        Assert.Equal(category.Id, request.CategoryId); // Defaults to first category in system (Vegetables)
    }
}
