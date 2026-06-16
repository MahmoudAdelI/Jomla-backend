using Jomla.Application.Common.Interfaces;
using MediatR;

namespace Jomla.Application.Features.Auth.Commands.Logout
{
    public class LogoutCommandHandler(IIdentityService _identityService) : IRequestHandler<LogoutCommand>
    {
        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var user = await _identityService.FindByRefreshTokenAsync(request.RefreshToken);
            if (user is null) return;

            var token = user.RefreshTokens.First(rt => rt.Token == request.RefreshToken);
            if (!token.IsActive) return;

            token.RevokedAt = DateTime.UtcNow;
            await _identityService.UpdateUserAsync(user);
        }
    }
}
