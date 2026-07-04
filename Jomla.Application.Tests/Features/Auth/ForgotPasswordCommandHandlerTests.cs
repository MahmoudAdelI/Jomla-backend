using Jomla.Application.Features.Auth.Commands.ForgotPassword;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using Jomla.Application.Tests.Infrastructure;
using NSubstitute;
using Xunit;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Tests.Features.Auth;

public class ForgotPasswordCommandHandlerTests : ApplicationTestBase
{
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _handler = new ForgotPasswordCommandHandler(
            IdentityService,
            JobDispatcher);
    }

    [Fact]
    public async Task Handle_EmailNotFound_SucceedsSilently()
    {
        // Arrange
        var email = "nonexistent@jomla.com";
        IdentityService.FindByEmailAsync(email).Returns((AppUser)null!);

        var command = new ForgotPasswordCommand(email);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Should not generate token or send email
        await IdentityService.DidNotReceiveWithAnyArgs().GeneratePasswordResetTokenAsync(Arg.Any<AppUser>());
        JobDispatcher.DidNotReceiveWithAnyArgs().Enqueue(Arg.Any<Expression<Func<IEmailService, Task>>>());
    }

    [Fact]
    public async Task Handle_EmailFound_GeneratesResetTokenAndEnqueuesEmailJob()
    {
        // Arrange
        var email = "user@jomla.com";
        var user = new AppUser { Id = Guid.NewGuid(), Email = email, FirstName = "Alice" };
        IdentityService.FindByEmailAsync(email).Returns(user);
        IdentityService.GeneratePasswordResetTokenAsync(user).Returns("reset_code_123");

        var command = new ForgotPasswordCommand(email);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await IdentityService.Received(1).GeneratePasswordResetTokenAsync(user);
        JobDispatcher.Received(1).Enqueue(Arg.Any<Expression<Func<IEmailService, Task>>>());
    }
}
