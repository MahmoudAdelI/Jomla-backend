using Jomla.Application.Features.Auth.Commands.Login;
using Jomla.Application.Features.Auth.DTOs;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using NSubstitute;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.Auth;

public class LoginCommandHandlerTests : ApplicationTestBase
{
    private readonly ITokenService _tokenService;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _tokenService = Substitute.For<ITokenService>();
        _handler = new LoginCommandHandler(IdentityService, _tokenService);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var email = "notfound@jomla.com";
        IdentityService.FindByEmailAsync(email).Returns((AppUser)null!);

        var command = new LoginCommand(email, "password123");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PasswordIncorrect_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var email = "user@jomla.com";
        var user = new AppUser { Id = Guid.NewGuid(), Email = email };
        IdentityService.FindByEmailAsync(email).Returns(user);
        IdentityService.CheckPasswordAsync(user, "wrongpassword").Returns(false);

        var command = new LoginCommand(email, "wrongpassword");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResponseDtoAndSavesRefreshToken()
    {
        // Arrange
        var email = "user@jomla.com";
        var user = new AppUser { Id = Guid.NewGuid(), Email = email, FirstName = "Alice", LastName = "Smith", ImageUrl = "avatar.jpg" };
        IdentityService.FindByEmailAsync(email).Returns(user);
        IdentityService.CheckPasswordAsync(user, "correctpassword").Returns(true);

        var roles = new List<string> { "Buyer" };
        IdentityService.GetUserRolesAsync(user).Returns(roles);

        _tokenService.GenerateToken(user, roles).Returns("jwt_token_xyz");
        
        var expectedRefreshToken = new RefreshToken
        {
            Token = "refresh_token_123",
            ExpiresOn = DateTime.UtcNow.AddDays(7)
        };
        _tokenService.GenerateRefreshToken().Returns(expectedRefreshToken);

        var command = new LoginCommand(email, "correctpassword");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("jwt_token_xyz", result.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(email, result.Email);
        Assert.Equal("Alice", result.FirstName);
        Assert.Equal("Smith", result.LastName);
        Assert.Equal("refresh_token_123", result.RefreshToken);
        Assert.Equal("avatar.jpg", result.ImageUrl);

        // Verify refresh token was added to user
        Assert.Contains(expectedRefreshToken, user.RefreshTokens);

        // Verify Identity service saved the user with the new token
        await IdentityService.Received(1).UpdateUserAsync(user);
    }
}
