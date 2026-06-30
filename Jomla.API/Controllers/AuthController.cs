using Jomla.Application.Features.Auth.Commands.Login;
using Jomla.Application.Features.Auth.Commands.Logout;
using Jomla.Application.Features.Auth.Commands.RefreshToken;
using Jomla.Application.Features.Auth.Commands.Register;
using Jomla.Application.Features.Auth.Commands.ForgotPassword;
using Jomla.Application.Features.Auth.Commands.ResetPassword;
using Jomla.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jomla.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(ISender _sender) : ControllerBase
    {
        [HttpPost("register")]
        [Produces("application/json")]
        [EndpointSummary("Register new user and return access token and refresh token")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            var result = await _sender.Send(command);
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresOn);
            return Ok(result);
        }

        [HttpPost("login")]
        [Produces("application/json")]
        [EndpointSummary("Login user and return access token and refresh token")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _sender.Send(command);
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresOn);
            return Ok(result);
        }

        [HttpPost("refresh")]
        [Produces("application/json")]
        [EndpointSummary("Issues a new access token and rotates the refresh token.")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = GetRefreshTokenCookie();

            if (string.IsNullOrEmpty(refreshToken))
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "Refresh token cookie is missing.");

            var result = await _sender.Send(new RefreshTokenCommand(refreshToken));
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresOn);
            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        [Produces("application/json")]
        [EndpointSummary("Logs the current user out and revokes their refresh token.")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = GetRefreshTokenCookie();

            if (!string.IsNullOrEmpty(refreshToken))
                await _sender.Send(new LogoutCommand(refreshToken));

            ClearRefreshTokenCookie();
            return NoContent();
        }

        [HttpPost("forgot-password")]
        [Produces("application/json")]
        [EndpointSummary("Sends a password reset email/code to the user if they exist.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
        {
            await _sender.Send(command);
            return Ok(new { message = "If the email is registered, a password reset code has been sent." });
        }

        [HttpPost("reset-password")]
        [Produces("application/json")]
        [EndpointSummary("Resets the user's password using the token sent via email.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            await _sender.Send(command);
            return Ok(new { message = "Password has been reset successfully." });
        }

        private const string RefreshTokenCookieName = "refreshToken";
        private string? GetRefreshTokenCookie()
        {
            return Request.Cookies[RefreshTokenCookieName];
        }

        private void ClearRefreshTokenCookie()
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(-1),
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            };
            Response.Cookies.Delete(RefreshTokenCookieName, cookieOptions);
        }
        private void SetRefreshTokenCookie(string refreshToken, DateTime expiresOn)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = expiresOn.ToUniversalTime(),
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
