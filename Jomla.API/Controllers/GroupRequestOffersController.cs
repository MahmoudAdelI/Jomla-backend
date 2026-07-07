using Hangfire;
using Jomla.Application.Features.GroupRequests.Commands.AcceptGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.CancelGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.PlaceGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.RejectGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.ApproveNegotiation;
using Jomla.Application.Features.GroupRequests.Commands.RejectNegotiation;
using Jomla.Application.Features.GroupRequests.Commands.TriggerNegotiation;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOffers;
using Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOfferDetail;
using Jomla.Application.Features.GroupRequests.Queries.GetSupplierGroupRequestOffers;
using Jomla.Domain;
using Jomla.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jomla.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupRequestOffersController(IMediator mediator,
 IBackgroundJobClient backgroundJobClient) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;

    [HttpPost("{id:guid}/accept")]
    [Produces("application/json")]
    [EndpointSummary("Buyer accepts a merchant's offer and creates a payment hold via Stripe.")]
    [ProducesResponseType(typeof(AcceptGroupRequestOfferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]

    public async Task<IActionResult> AcceptOffer(Guid id, [FromBody] AcceptOfferRequest request)
    {
        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var buyerEmail = User.FindFirstValue(ClaimTypes.Email)!;

        var result = await _mediator.Send(

        new AcceptGroupRequestOfferCommand(id, buyerId, buyerEmail, request.AcceptedQuantity));

        return Ok(result);
    }

    [HttpPost("{id:guid}/confirm-accept")]
    [Produces("application/json")]
    [EndpointSummary("Confirm buyer acceptance of a merchant's offer after Stripe payment completes.")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmAcceptOffer(Guid id, [FromBody] ConfirmAcceptOfferRequest request)
    {
        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var buyerEmail = User.FindFirstValue(ClaimTypes.Email)!;

        var result = await _mediator.Send(
            new ConfirmAcceptGroupRequestOfferCommand(
                id,
                buyerId,
                buyerEmail,
                request.AcceptedQuantity,
                request.PaymentIntentId,
                request.ShippingAddress,
                request.PhoneNumber));

        return Ok(result);
    }



    [HttpPost("{id:guid}/reject")]
    [Produces("application/json")]
    [EndpointSummary("Buyer rejects a merchant's offer, voting to trigger AI price negotiation.")]
    [ProducesResponseType(typeof(RejectGroupRequestOfferResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectOffer(Guid id)
    {
        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new RejectGroupRequestOfferCommand(id, buyerId));
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve-negotiation")]
    [Authorize(Roles = Roles.Supplier)]
    [Produces("application/json")]
    [EndpointSummary("Supplier approves the AI-proposed price negotiation counter-offer.")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveNegotiation(Guid id)
    {
        var supplierId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new ApproveNegotiationCommand(id, supplierId));
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject-negotiation")]
    [Authorize(Roles = Roles.Supplier)]
    [Produces("application/json")]
    [EndpointSummary("Supplier rejects the AI-proposed price negotiation counter-offer, failing the offer.")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectNegotiation(Guid id)
    {
        var supplierId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new RejectNegotiationCommand(id, supplierId));
        return Ok(result);
    }

    [HttpPost("{id:guid}/trigger-negotiation")]
    [Authorize(Roles = Roles.Supplier)]
    [Produces("application/json")]
    [EndpointSummary("Supplier triggers immediate negotiation expiry flow for testing/demo purposes.")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerNegotiation(Guid id)
    {
        var supplierId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new TriggerNegotiationCommand(id, supplierId));
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Produces("application/json")]
    [EndpointSummary("Buyer cancels their accepted offer, triggering an asynchronous Stripe release and database update.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult CancelOffer(Guid id)
    {
        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        _backgroundJobClient.Enqueue<ISender>(sender =>
            sender.Send(new CancelGroupRequestOfferCommand(id, buyerId), CancellationToken.None));

        return Accepted(new { Success = true });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Supplier)]
    [Produces("application/json")]
    [EndpointSummary("Get detailed information of a group request offer for the supplier, including buyer responses.")]
    [ProducesResponseType(typeof(SupplierGroupRequestOfferDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOfferDetail(Guid id)
    {
        var result = await _mediator.Send(new GetGroupRequestOfferDetailQuery(id));
        return Ok(result);
    }

    [HttpGet("my-offers")]
    [Authorize(Roles = Roles.Supplier)]
    [Produces("application/json")]
    [EndpointSummary("Get all group request offers placed by the logged-in supplier.")]
    [ProducesResponseType(typeof(PagedResult<SupplierGroupRequestOfferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOffers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var supplierId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new GetSupplierGroupRequestOffersQuery(supplierId, page, pageSize, search, status));
        return Ok(result);
    }
}
public class AcceptOfferRequest
{
    public int AcceptedQuantity { get; set; }
}

public class ConfirmAcceptOfferRequest
{
    public int AcceptedQuantity { get; set; }
    public string PaymentIntentId { get; set; } = null!;
    public string? ShippingAddress { get; set; }
    public string? PhoneNumber { get; set; }
}