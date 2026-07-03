using CloudinaryDotNet.Actions;
using Jomla.Application.Features.Offers.Commands.CreateOffer;
using Jomla.Application.Features.Offers.Commands.DeactivateOffer;
using Jomla.Application.Features.Offers.Commands.DeleteOffer;
using Jomla.Application.Features.Offers.Commands.UpdateOffer;
using Jomla.Application.Features.Offers.Queries.GetAllOffers;
using Jomla.Application.Features.Offers.Queries.GetMyOffers;
using Jomla.Application.Features.Offers.Queries.GetOfferById;
using Jomla.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace Jomla.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OffersController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Supplier))]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] CreateOfferCommand command)
    {
        var offerId = await _mediator.Send(command);

        return Ok(new
        {
            Success = true,
            OfferId = offerId
        });
    }


    [HttpGet]
    public async Task<IActionResult> GetAllOffers(
        [FromQuery] GetAllOffersQuery query)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? currentUserId = string.IsNullOrEmpty(userIdString) ? null : Guid.Parse(userIdString);
        query = query with { CurrentUserId = currentUserId };

        var result = await mediator.Send(query);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(
            new GetOfferByIdQuery(id));

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Supplier))]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
    Guid id,
    [FromForm] UpdateOfferCommand command)
    {
        command = command with { Id = id };

        var result = await _mediator.Send(command);

        return Ok(new
        {
            Success = result
        });
    }


    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Supplier))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(
            new DeleteOfferCommand(id));

        return Ok(new
        {
            Success = result
        });
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = nameof(UserRole.Supplier))]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _mediator.Send(new DeactivateOfferCommand(id));

        return Ok(new { Success = result });
    }

    [HttpGet("my-offers")]
    [Authorize(Roles = nameof(UserRole.Supplier))]
    public async Task<IActionResult> GetMyOffers(
        [FromQuery] GetMyOffersQuery query)
    {
        var result = await mediator.Send(query);

        return Ok(result);
    }
}