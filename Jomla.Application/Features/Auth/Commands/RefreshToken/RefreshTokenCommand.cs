using Jomla.Application.Features.Auth.DTOs;
using MediatR;

namespace Jomla.Application.Features.Auth.Commands.RefreshToken
{
    public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;
}
