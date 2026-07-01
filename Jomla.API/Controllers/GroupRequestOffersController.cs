using Hangfire;
using Jomla.Application.Features.GroupRequests.Commands.AcceptGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.CancelGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.PlaceGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.RejectGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Dtos;
using Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOffers;
using Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOfferDetail;
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
 IBackgroundJobClient backgroundJobClient ) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IBackgroundJobClient _backgroundJobClient= backgroundJobClient;

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
    [ProducesResponseType(typeof(ConfirmAcceptGroupRequestOfferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmAcceptOffer(Guid id, [FromBody] ConfirmAcceptOfferRequest request)
    {
        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var buyerEmail = User.FindFirstValue(ClaimTypes.Email)!;

        var result = await _mediator.Send(
            new ConfirmAcceptGroupRequestOfferCommand(id, buyerId, buyerEmail, request.AcceptedQuantity, request.PaymentIntentId));

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
}

public class AcceptOfferRequest
{
    public int AcceptedQuantity { get; set; }
}

public class ConfirmAcceptOfferRequest
{
    public int AcceptedQuantity { get; set; }
    public string PaymentIntentId { get; set; } = null!;
}