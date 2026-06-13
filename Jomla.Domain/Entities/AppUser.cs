using Microsoft.AspNetCore.Identity;

namespace Jomla.Domain.Entities
{
    public class AppUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    }
}
