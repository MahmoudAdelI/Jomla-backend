using Hangfire;
using Jomla.Application.Features.GroupRequests.Commands.AcceptGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.CancelGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.FailGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.CompleteGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.ExpireGroupRequestOffer;
using Jomla.Application.Features.GroupRequests.Commands.RejectGroupRequestOffer;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;

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
    public async Task<IActionResult> AcceptOffer(Guid id)
    {
        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var buyerEmail = User.FindFirstValue(ClaimTypes.Email)!;

        var result = await _mediator.Send(
            new AcceptGroupRequestOfferCommand(id, buyerId, buyerEmail));

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

    //[HttpPost("{id:guid}/complete")]
    //[Produces("application/json")]
    //[EndpointSummary("Manually trigger the completion of an offer and capture payments.")]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //public async Task<IActionResult> CompleteOffer(Guid id)
    //{
    //    await _mediator.Send(new CompleteGroupRequestOfferCommand(id));
    //    return Ok(new { Success = true });
    //}

    //[HttpPost("{id:guid}/cancel")]
    //[Produces("application/json")]
    //[EndpointSummary("Manually fail an offer and release all Stripe payment holds.")]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //public async Task<IActionResult> CancelOffer(Guid id)
    //{
    //    await _mediator.Send(new FailGroupRequestOfferCommand(id));
    //    return Ok(new { Success = true });
    //}

    //[HttpPost("{id:guid}/expire")]
    //[Produces("application/json")]
    //[EndpointSummary("Manually trigger the expiration process for a timed-out offer.")]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //public async Task<IActionResult> ExpireOffer(Guid id)
    //{
    //    await _mediator.Send(new ExpireGroupRequestOfferCommand(id));
    //    return Ok(new { Success = true });
    //}
    
    [HttpPost("{id:guid}/cancel")]
    [Produces("application/json")]
    [EndpointSummary("Buyer cancels their accepted offer, triggering an asynchronous Stripe release and database update.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult LeaveOffer(Guid id)
    {
        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 
        _backgroundJobClient.Enqueue<ISender>(sender =>
            sender.Send(new CancelGroupRequestOfferCommand(id, buyerId), CancellationToken.None));

        return Accepted(new { Success = true });
    }

    [HttpPost("{id:guid}/offers")]
    [Authorize(Roles = nameof(UserRole.Supplier))]
    public async Task<IActionResult> PlaceOffer(Guid requestId,[FromBody] PlaceGroupRequestOfferCommand command)
    {
        command.GroupRequestId = requestId;

        var offerId = await _mediator.Send(command);

        return Ok(new
        {
            Success = true,
            OfferId = offerId
        });
    }


}