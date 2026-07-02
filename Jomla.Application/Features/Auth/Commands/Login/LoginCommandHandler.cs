using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Auth.DTOs;
using MediatR;

namespace Jomla.Application.Features.Auth.Commands.Login
{
    public class LoginCommandHandler(
        IIdentityService _identityService,
        ITokenService _tokenService
        ) : IRequestHandler<LoginCommand, AuthResponseDto>
    {
        public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _identityService.FindByEmailAsync(request.Email);
            if (user is null || !await _identityService.CheckPasswordAsync(user, request.Password))
                throw new UnauthorizedAccessException("Invalid email or password");

            var roles = await _identityService.GetUserRolesAsync(user);

            var token = _tokenService.GenerateToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshTokens.Add(refreshToken);
            await _identityService.UpdateUserAsync(user);

            return new AuthResponseDto(
                token,
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                refreshToken.Token,
                refreshToken.ExpiresOn,
                user.ImageUrl
            );
        }
    }
}
