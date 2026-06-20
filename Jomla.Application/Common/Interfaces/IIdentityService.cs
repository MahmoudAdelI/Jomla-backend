using Jomla.Domain;
using Jomla.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Jomla.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        public Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(AppUser user, string password, UserRole role);

        public Task<IList<string>> GetUserRolesAsync(AppUser user);

        public Task<IdentityResult> UpdateUserAsync(AppUser user);

        public Task<AppUser?> FindByEmailAsync(string email);

        public Task<bool> CheckPasswordAsync(AppUser user, string password);

        public Task<AppUser?> FindByRefreshTokenAsync(string refreshToken);

        Guid GetCurrentUserId();
        string GetCurrentUserEmail();
    }
}
