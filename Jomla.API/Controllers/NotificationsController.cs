using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications.Commands.MarkAllAsRead;
using Jomla.Application.Features.Notifications.Commands.MarkAsRead;
using Jomla.Application.Features.Notifications.Queries.GetNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jomla.API.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController(IMediator mediator, IIdentityService identityService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IIdentityService _identityService = identityService;

    /// <summary>
    /// Get the current user's notifications, ordered by most recent first.
    /// Supports optional filtering by unread-only and standard pagination.
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [EndpointSummary("Get paginated notifications for the current user with optional unread filter")]
    [ProducesResponseType(typeof(GetNotificationsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = _identityService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(
            new GetNotificationsQuery(userId, unreadOnly, page, pageSize));

        return Ok(result);
    }

    /// <summary>
    /// Mark a single notification as read. Ownership is enforced — users can
    /// only mark their own notifications.
    /// </summary>
    [HttpPatch("{notificationId:guid}/read")]
    [Produces("application/json")]
    [EndpointSummary("Mark a single notification as read")]
    [ProducesResponseType(typeof(MarkAsReadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        var userId = _identityService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new MarkAsReadCommand(notificationId, userId));

        if (!result.Success)
            return NotFound(new { result.Error });

        return Ok(result);
    }

    /// <summary>
    /// Mark all of the current user's unread notifications as read in one request.
    /// Returns how many notifications were marked so the client can reset the badge.
    /// </summary>
    [HttpPatch("read-all")]
    [Produces("application/json")]
    [EndpointSummary("Mark all unread notifications as read for the current user")]
    [ProducesResponseType(typeof(MarkAllAsReadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = _identityService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new MarkAllAsReadCommand(userId));

        return Ok(result);
    }
}
