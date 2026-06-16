using MediatR;

namespace Jomla.Application.Features.Auth.Commands.Logout
{
    public sealed record LogoutCommand(string RefreshToken) : IRequest;
}
