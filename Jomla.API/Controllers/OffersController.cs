using Jomla.Application.Features.Offers.Commands.CreateOffer;
using Jomla.Application.Features.Offers.Commands.DeleteOffer;
using Jomla.Application.Features.Offers.Commands.UpdateOffer;
using Jomla.Application.Features.Offers.Queries.GetAllOffers;
using Jomla.Application.Features.Offers.Queries.GetMyOffers;
using Jomla.Application.Features.Offers.Queries.GetOfferById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jomla.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Supplier")]
public class OffersController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] CreateOfferCommand command)
    {
        var offerId = await mediator.Send(command);

        return Ok(new
        {
            Success = true,
            OfferId = offerId
        });
    }


    [HttpGet]
    public async Task<IActionResult> GetAllOffers()
    {
        var result = await _mediator.Send(
            new GetAllOffersQuery());

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await mediator.Send(
            new GetOfferByIdQuery(id));

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
    Guid id,
    [FromForm] UpdateOfferCommand command)
    {
        command = command with { Id = id };

        var result = await mediator.Send(command);

        return Ok(new
        {
            Success = result
        });
    }


    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await mediator.Send(
            new DeleteOfferCommand(id));

        return Ok(new
        {
            Success = result
        });
    }

    [HttpGet("my-offers")]
    public async Task<IActionResult> GetMyOffers()
    {
        var result = await mediator.Send(
            new GetMyOffersQuery());

        return Ok(result);
    }
}