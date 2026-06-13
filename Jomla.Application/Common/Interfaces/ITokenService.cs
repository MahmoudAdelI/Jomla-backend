using Jomla.Domain.Entities;

namespace Jomla.Application.Common.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(AppUser user, IList<string> roles);
        RefreshToken GenerateRefreshToken();
    }
}
