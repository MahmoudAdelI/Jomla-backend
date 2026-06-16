using Jomla.Application.Features.Auth.DTOs;
using MediatR;

namespace Jomla.Application.Features.Auth.Commands.Login
{
    public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;
}
