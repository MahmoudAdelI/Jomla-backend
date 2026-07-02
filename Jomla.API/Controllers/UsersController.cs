using Jomla.Application.Features.Users.Commands.ChangePassword;
using Jomla.Application.Features.Users.Commands.UpdateProfile;
using Jomla.Application.Features.Users.Commands.UpdateProfileImage;
using Jomla.Application.Features.Users.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jomla.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(ISender _sender) : ControllerBase
    {
        [HttpPut("profile")]
        [Produces("application/json")]
        [EndpointSummary("Updates the current user's profile info.")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            // Step 1: Pull the user ID from the JWT claims
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Step 2: Build and send the command
            var command = new UpdateProfileCommand
            {
                UserId = userId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };

            var result = await _sender.Send(command);
            return Ok(result);
        }

        [HttpPut("change-password")]
        [Produces("application/json")]
        [EndpointSummary("Changes the current user's password.")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var command = new ChangePasswordCommand
            {
                UserId = userId,
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            };

            await _sender.Send(command);
            return NoContent();
        }

        [HttpPut("profile-image")]
        [Produces("application/json")]
        [EndpointSummary("Uploads and updates the current user's profile image.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfileImage(
        IFormFile file,
        CancellationToken cancellationToken)
        {
            // Step 1: Get the current user's ID from the JWT claims
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Step 2: Build and send the command
            var command = new UpdateProfileImageCommand
            {
                UserId = userId,
                File = file
            };

            var imageUrl = await _sender.Send(command, cancellationToken);

            // Step 3: Return the new image URL
            return Ok(new { ImageUrl = imageUrl });
        }
        public record UpdateProfileRequest(string FirstName, string LastName, string Email);
        public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
    }
}