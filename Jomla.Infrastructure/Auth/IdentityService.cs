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

        public async Task<AppUser?> FindByEmailAsync(string email) => 
            await _userMgr.Users
                .Include(u => u.ContactInfo)
                .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant());

        public async Task<AppUser?> FindByRefreshTokenAsync(string refreshToken)
        {
            return await _userMgr.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.ContactInfo)
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
                ?.FindFirst(ClaimTypes.Email)?.Value ?? "";
        }

        public async Task<AppUser?> FindByIdAsync(Guid userId) => 
            await _userMgr.Users
                .Include(u => u.ContactInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

        public async Task<bool> IsInRoleAsync(AppUser user, string role) => await _userMgr.IsInRoleAsync(user, role);

        public async Task<string> GeneratePasswordResetTokenAsync(AppUser user) => await _userMgr.GeneratePasswordResetTokenAsync(user);

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> ResetPasswordAsync(AppUser user, string token, string newPassword)
        {
            var result = await _userMgr.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
                return (true, Enumerable.Empty<string>());
            return (false, result.Errors.Select(e => e.Description));
        }
    }
}