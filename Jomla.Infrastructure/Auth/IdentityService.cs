using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Jomla.Infrastructure.Auth
{
    public class IdentityService(
        UserManager<AppUser> _userMgr,
        IHttpContextAccessor _httpContextAccessor) : IIdentityService  // ← أضيف IHttpContextAccessor
    {
        public Task<bool> CheckPasswordAsync(AppUser user, string password) => _userMgr.CheckPasswordAsync(user, password);

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(AppUser user, string password, UserRole role)
        {
            var result = await _userMgr.CreateAsync(user, password);
            if (!result.Succeeded)
                return (false, result.Errors.Select(e => e.Description));
            await _userMgr.AddToRoleAsync(user, role.ToString());
            return (true, Enumerable.Empty<string>());
        }

        public async Task<AppUser?> FindByEmailAsync(string email) => await _userMgr.FindByEmailAsync(email);

        public async Task<AppUser?> FindByRefreshTokenAsync(string refreshToken)
        {
            return await _userMgr.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken));
        }

        public async Task<IList<string>> GetUserRolesAsync(AppUser user) => await _userMgr.GetRolesAsync(user);

        public async Task<IdentityResult> UpdateUserAsync(AppUser user) => await _userMgr.UpdateAsync(user);

        //for batchController
        public Guid GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                ?.FindFirst("sub")?.Value
                ?? _httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;

            return Guid.Empty;
        }

        public string GetCurrentUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User
                ?.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? "";
        }
    }
}