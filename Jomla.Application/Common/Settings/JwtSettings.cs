namespace Jomla.Application.Common.Settings
{
    public class JwtSettings
    {
        public string Key { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public int ExpiresInMinutes { get; init; } = 15;
        public int RefreshTokenExpiresInDays { get; init; } = 7;
    }
}
