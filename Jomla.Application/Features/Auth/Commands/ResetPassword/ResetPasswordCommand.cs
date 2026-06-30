using MediatR;

namespace Jomla.Application.Features.Auth.Commands.ResetPassword
{
    public record ResetPasswordCommand(string Email, string Token, string NewPassword, string ConfirmPassword) : IRequest;
}
