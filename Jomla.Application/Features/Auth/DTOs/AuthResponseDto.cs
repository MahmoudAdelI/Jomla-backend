using System.Text.Json.Serialization;

namespace Jomla.Application.Features.Auth.DTOs
{
    public record AuthResponseDto(
        string Token,
        Guid UserId,
        string Email,
        string FirstName,
        string LastName,
        [property: JsonIgnore] string RefreshToken,
        DateTime RefreshTokenExpiresOn
        );
}
