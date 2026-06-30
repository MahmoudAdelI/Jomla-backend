using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.GroupRequests.Commands.CreateGroupRequest;
using Jomla.Application.Features.GroupRequests.Commands.JoinGroupRequest;
using Jomla.Application.Features.GroupRequests.Commands.LeaveGroupRequest;
using Jomla.Application.Features.GroupRequests.Commands.PlaceGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Application.Features.GroupRequests.Queries;
using Jomla.Application.Features.GroupRequests.Queries.GetGroupRequests;
using Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOffers;
using Jomla.Application.Features.GroupRequests.Queries.GetSupplierMatchedGroupRequests;
using Jomla.Domain;
using Jomla.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jomla.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/group-requests")]
    public class GroupRequestsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IIdentityService _identityService;

        public GroupRequestsController(IMediator mediator, IIdentityService identityService)
        {
            _mediator = mediator;
            _identityService = identityService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [EndpointSummary("Create a new group request with AI category resolution and background moderation")]
        [ProducesResponseType(typeof(CreateGroupRequestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateGroupRequest([FromForm] CreateGroupRequestRequest request)
        {
            var buyerId = _identityService.GetCurrentUserId();
            if (buyerId == Guid.Empty) return Unauthorized();

            var command = new CreateGroupRequestCommand(
                buyerId,
                request.Title,
                request.Quantity,
                request.Description,
                request.Images);

            var result = await _mediator.Send(command);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("{id}")]
        [Produces("application/json")]
        [EndpointSummary("Get a single group request with all its details")]
        [ProducesResponseType(typeof(GroupRequestDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetGroupRequest(Guid id)
        {
            var result = await _mediator.Send(new GetGroupRequestDetailQuery(id));

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        [Produces("application/json")]
        [EndpointSummary("Get active and approved group requests sorted by highest quantity with pagination and filtering")]
        [ProducesResponseType(typeof(PagedResult<GroupRequestListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetGroupRequests(
            [FromQuery] Guid? categoryId,
            [FromQuery] string? titleSearch,
            [FromQuery] string? status,
            [FromQuery] string? sortBy,
            [FromQuery] bool? myRequestsOnly,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            Guid? filterBuyerId = null;
            if (myRequestsOnly == true)
            {
                filterBuyerId = _identityService.GetCurrentUserId();
                if (filterBuyerId == Guid.Empty) return Unauthorized();
            }

            var result = await _mediator.Send(new GetGroupRequestsQuery(
                categoryId, titleSearch, status, page, pageSize, sortBy, filterBuyerId));

            return Ok(result);
        }

        [HttpPost("{id}/join")]
        [Produces("application/json")]
        [EndpointSummary("Join a group request as a buyer and trigger supplier matching in the background")]
        [ProducesResponseType(typeof(JoinGroupRequestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> JoinGroupRequest(Guid id, [FromBody] JoinGroupRequestRequest request)
        {
            var buyerId = _identityService.GetCurrentUserId();
            if (buyerId == Guid.Empty) return Unauthorized();

            var result = await _mediator.Send(new JoinGroupRequestCommand(id, buyerId, request.Quantity));

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("{id}/leave")]
        [Produces("application/json")]
        [EndpointSummary("Leave a group request and schedule auto-close job if quantity drops to zero")]
        [ProducesResponseType(typeof(LeaveGroupRequestResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LeaveGroupRequest(Guid id)
        {
            var buyerId = _identityService.GetCurrentUserId();
            if (buyerId == Guid.Empty) return Unauthorized();

            var result = await _mediator.Send(new LeaveGroupRequestCommand(id, buyerId));

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("matched")]
        [Produces("application/json")]
        [EndpointSummary("Get group requests that matched the supplier's category preferences")]
        [ProducesResponseType(typeof(PagedResult<SupplierMatchedGroupRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMatchedGroupRequests(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var supplierId = _identityService.GetCurrentUserId();
            if (supplierId == Guid.Empty) return Unauthorized();

            var result = await _mediator.Send(new GetSupplierMatchedGroupRequestsQuery(
                supplierId, page, pageSize));
 
            return Ok(result);
        }

        [HttpPost("{requestId:guid}/offers")]
        [Authorize(Roles = Roles.Supplier)]
        [Produces("application/json")]
        [EndpointSummary("Supplier places an offer on a buyer's group request.")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> PlaceOffer(
            Guid requestId,
            [FromBody] PlaceGroupRequestOfferCommand command)
        {
            var supplierId = _identityService.GetCurrentUserId();
            if (supplierId == Guid.Empty) return Unauthorized();

            command.SupplierId = supplierId;
            command.GroupRequestId = requestId;

            var offerId = await _mediator.Send(command);
            return Ok(new { Success = true, OfferId = offerId });
        }

        [HttpGet("{requestId:guid}/offers")]
        [Produces("application/json")]
        [EndpointSummary("Get group request offers with optional status filtering, sorted by price ascending.")]
        [ProducesResponseType(typeof(PagedResult<BuyerGroupRequestOfferDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOffers(
            Guid requestId,
            [FromQuery] GroupRequestOfferStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _mediator.Send(new GetGroupRequestOffersQuery
            {
                GroupRequestId = requestId,
                Status = status,
                Page = page,
                PageSize = pageSize
            });
            return Ok(result);
        }
    }

    public class CreateGroupRequestRequest
    {
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Description { get; set; }
        public List<IFormFile>? Images { get; set; }
    }

    public class JoinGroupRequestRequest
    {
        public int Quantity { get; set; }
    }
}
