using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Auth.DTOs;
using MediatR;

namespace Jomla.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler(
        IIdentityService _identityService,
        ITokenService _tokenService
        ) : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
    {
        public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await _identityService.FindByRefreshTokenAsync(request.RefreshToken)
                ?? throw new UnauthorizedAccessException("Invalid refresh token.");

            var existingToken = user.RefreshTokens.First(rt => rt.Token == request.RefreshToken);
            if (!existingToken.IsActive) throw new UnauthorizedAccessException("Refresh token is expired or revoked.");

            existingToken.RevokedAt = DateTime.UtcNow;

            var roles = await _identityService.GetUserRolesAsync(user);
            var token = _tokenService.GenerateToken(user, roles);

            var newRefreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            await _identityService.UpdateUserAsync(user);

            return new AuthResponseDto(
                token,
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                newRefreshToken.Token,
                newRefreshToken.ExpiresOn
            );
        }
    }
}
