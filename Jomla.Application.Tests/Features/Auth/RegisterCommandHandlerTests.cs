using AutoMapper;
using FluentValidation;
using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Auth.Commands.Register;
using Jomla.Application.Features.Auth.DTOs;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using NSubstitute;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.Auth;

public class RegisterCommandHandlerTests : ApplicationTestBase
{
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _tokenService = Substitute.For<ITokenService>();
        _mapper = Substitute.For<IMapper>();
        _handler = new RegisterCommandHandler(
            IdentityService,
            _tokenService,
            _mapper);
    }

    [Fact]
    public async Task Handle_RegisterAsAdmin_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var command = new RegisterCommand("First", "Last", "admin@jomla.com", "Pass123!", "Pass123!", UserRole.Admin);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Cannot register as Admin.", ex.Message);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var email = "existing@jomla.com";
        var command = new RegisterCommand("First", "Last", email, "Pass123!", "Pass123!", UserRole.Buyer);
        IdentityService.FindByEmailAsync(email).Returns(new AppUser { Email = email });

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CreateUserFails_ThrowsValidationException()
    {
        // Arrange
        var email = "new@jomla.com";
        var command = new RegisterCommand("First", "Last", email, "Pass123!", "Pass123!", UserRole.Buyer);
        IdentityService.FindByEmailAsync(email).Returns((AppUser)null!);

        var user = new AppUser { Email = email };
        _mapper.Map<AppUser>(command).Returns(user);

        IdentityService.CreateUserAsync(user, command.Password, command.Role)
            .Returns((false, new List<string> { "Password too weak", "Email invalid" }));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.NotEmpty(ex.Errors);
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesUserAndReturnsTokens()
    {
        // Arrange
        var email = "success@jomla.com";
        var command = new RegisterCommand("John", "Doe", email, "Pass123!", "Pass123!", UserRole.Buyer);
        IdentityService.FindByEmailAsync(email).Returns((AppUser)null!);

        var user = new AppUser { Id = Guid.NewGuid(), Email = email, FirstName = "John", LastName = "Doe" };
        _mapper.Map<AppUser>(command).Returns(user);

        IdentityService.CreateUserAsync(user, command.Password, command.Role)
            .Returns((true, new List<string>()));

        var roles = new List<string> { "Buyer" };
        IdentityService.GetUserRolesAsync(user).Returns(roles);

        _tokenService.GenerateToken(user, roles).Returns("jwt_token_123");
        
        var refreshToken = new RefreshToken
        {
            Token = "refresh_token_abc",
            ExpiresOn = DateTime.UtcNow.AddDays(7)
        };
        _tokenService.GenerateRefreshToken().Returns(refreshToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("jwt_token_123", result.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(email, result.Email);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("refresh_token_abc", result.RefreshToken);

        // Verify tokens added to user and user updated
        Assert.Contains(refreshToken, user.RefreshTokens);
        await IdentityService.Received(1).UpdateUserAsync(user);
    }
}
