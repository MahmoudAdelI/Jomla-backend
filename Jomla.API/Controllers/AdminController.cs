using Jomla.Application.Common.BaseClass;
using Jomla.Application.Features.Admin.Commands.ApproveGroupRequest;
using Jomla.Application.Features.Admin.Commands.ApproveOffer;
using Jomla.Application.Features.Admin.Commands.RejectGroupRequest;
using Jomla.Application.Features.Admin.Commands.RejectOffer;
using Jomla.Application.Features.Admin.Dtos;
using Jomla.Application.Features.Admin.Queries.GetFlaggedGroupRequests;
using Jomla.Application.Features.Admin.Queries.GetFlaggedOffers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jomla.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("flagged-offers")]
        [Produces("application/json")]
        [EndpointSummary("Get all flagged offers for admin review")]
        [ProducesResponseType(typeof(PagedResponse<FlaggedOfferDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetFlaggedOffers([FromQuery] GetFlaggedOffersQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("flagged-group-requests")]
        [Produces("application/json")]
        [EndpointSummary("Get all flagged group requests for admin review")]
        [ProducesResponseType(typeof(PagedResponse<FlaggedGroupRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetFlaggedGroupRequests([FromQuery] GetFlaggedGroupRequestsQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPut("offers/{id}/approve")]
        [Produces("application/json")]
        [EndpointSummary("Approve a flagged offer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ApproveOffer(Guid id)
        {
            await _mediator.Send(new ApproveOfferCommand(id));
            return NoContent();
        }

        [HttpPut("offers/{id}/reject")]
        [Produces("application/json")]
        [EndpointSummary("Reject a flagged offer with a reason")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RejectOffer(Guid id, [FromBody] AdminRejectRequest request)
        {
            await _mediator.Send(new RejectOfferCommand(id, request.Reason));
            return NoContent();
        }

        [HttpPut("group-requests/{id}/approve")]
        [Produces("application/json")]
        [EndpointSummary("Approve a flagged group request")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ApproveGroupRequest(Guid id)
        {
            await _mediator.Send(new ApproveGroupRequestCommand(id));
            return NoContent();
        }

        [HttpPut("group-requests/{id}/reject")]
        [Produces("application/json")]
        [EndpointSummary("Reject a flagged group request with a reason")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RejectGroupRequest(Guid id, [FromBody] AdminRejectRequest request)
        {
            await _mediator.Send(new RejectGroupRequestCommand(id, request.Reason));
            return NoContent();
        }
    }

    public class AdminRejectRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
