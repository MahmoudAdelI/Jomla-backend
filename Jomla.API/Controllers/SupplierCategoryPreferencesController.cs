using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.SupplierCategoryPreferences.Commands.RemoveSupplierCategoryPreference;
using Jomla.Application.Features.SupplierCategoryPreferences.Commands.SaveSupplierCategoryPreference;
using Jomla.Application.Features.SupplierCategoryPreferences.DTOs;
using Jomla.Application.Features.SupplierCategoryPreferences.Queries.GetSupplierCategoryPreferences;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jomla.API.Controllers;

[Authorize]
[ApiController]
[Route("api/supplier-category-preferences")]
public class SupplierCategoryPreferencesController(IMediator mediator, IIdentityService identityService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IIdentityService _identityService = identityService;

    /// <summary>
    /// Retrieve all category preferences for the authenticated supplier.
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [EndpointSummary("Get all category preferences for the authenticated supplier")]
    [ProducesResponseType(typeof(List<SupplierCategoryPreferenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences()
    {
        var supplierId = _identityService.GetCurrentUserId();
        if (supplierId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new GetSupplierCategoryPreferencesQuery(supplierId));
        return Ok(result);
    }

    /// <summary>
    /// Save or update a category preference for the authenticated supplier.
    /// </summary>
    [HttpPut]
    [Produces("application/json")]
    [EndpointSummary("Save or update a category preference")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SavePreference([FromBody] SavePreferenceRequest request)
    {
        var supplierId = _identityService.GetCurrentUserId();
        if (supplierId == Guid.Empty) return Unauthorized();

        var command = new SaveSupplierCategoryPreferenceCommand(supplierId, request.CategoryId, request.MinQuantity);
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Remove a category preference for the authenticated supplier.
    /// </summary>
    [HttpDelete("{categoryId:guid}")]
    [Produces("application/json")]
    [EndpointSummary("Remove a category preference")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePreference(Guid categoryId)
    {
        var supplierId = _identityService.GetCurrentUserId();
        if (supplierId == Guid.Empty) return Unauthorized();

        var command = new RemoveSupplierCategoryPreferenceCommand(supplierId, categoryId);
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}

public class SavePreferenceRequest
{
    public Guid CategoryId { get; set; }
    public int MinQuantity { get; set; }
}
