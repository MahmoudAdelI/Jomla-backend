using Jomla.Application.Features.Auth.DTOs;
using Jomla.Domain;
using MediatR;

namespace Jomla.Application.Features.Auth.Commands.Register
{
    public sealed record RegisterCommand(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string ConfirmPassword,
        UserRole Role
        ) : IRequest<AuthResponseDto>;
}
