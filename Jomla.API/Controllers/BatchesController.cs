using Hangfire;
using Jomla.Application.Features.Batches.Commands;
using Jomla.Application.Features.Batches.Commands.UpdateBatch;
using Jomla.Application.Features.Batches.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Jomla.Application.Features.Batches.Queries.SearchBatches;

namespace Jomla.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BatchesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// GET /api/batches/search
    /// </summary>
    [HttpGet("search")]
    [EndpointSummary("Search and paginate batches by status or query term.")]
    [ProducesResponseType(typeof(PagedBatchesResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchBatches(
        [FromQuery] string? searchTerm,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new SearchBatchesQuery(searchTerm, status, page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// GET /api/batches/{batchId}
    /// </summary>
    [HttpGet("{batchId:guid}")]
    [EndpointSummary("Get batch detail with participants.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBatch(Guid batchId)
    {
        var result = await _mediator.Send(new GetBatchDetailQuery(batchId));
        return Ok(result);
    }

    /// <summary>
    /// POST /api/batches/{batchId}/join
    /// </summary>
    [HttpPost("{batchId:guid}/join")]
    [EndpointSummary("Buyer joins a batch and creates a Stripe payment hold.")]
    [ProducesResponseType(typeof(JoinBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinBatch(Guid batchId, [FromBody] JoinBatchRequest request)
    {
        if (request?.Quantity <= 0)
            return BadRequest(new { success = false, error = "Quantity must be greater than 0." });

        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var buyerEmail = User.FindFirstValue(ClaimTypes.Email)!;

        var result = await _mediator.Send(new JoinBatchCommand
        {
            BatchId = batchId,
            BuyerId = buyerId,
            BuyerEmail = buyerEmail,
            Quantity = request.Quantity
        });

        if (!result.Success)
        {
            if (result.StatusCode.HasValue)
                return StatusCode(result.StatusCode.Value, result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// POST /api/batches/{batchId}/confirm-join
    /// </summary>
    [HttpPost("{batchId:guid}/confirm-join")]
    [EndpointSummary("Confirms user joining a batch after successful Stripe payment hold.")]
    [ProducesResponseType(typeof(ConfirmJoinBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmJoinBatch(Guid batchId, [FromBody] ConfirmJoinBatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.PaymentIntentId))
            return BadRequest(new { success = false, error = "PaymentIntentId is required." });

        if (request.Quantity <= 0)
            return BadRequest(new { success = false, error = "Quantity must be greater than 0." });

        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _mediator.Send(new ConfirmJoinBatchCommand
        {
            BatchId = batchId,
            BuyerId = buyerId,
            Quantity = request.Quantity,
            PaymentIntentId = request.PaymentIntentId
        });

        if (!result.Success)
        {
            if (result.StatusCode.HasValue)
                return StatusCode(result.StatusCode.Value, result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// PUT /api/batches/{batchId}/quantity
    /// </summary>
    [HttpPut("{batchId:guid}/quantity")]
    [EndpointSummary("Buyer updates their quantity in a batch, cycling the Stripe payment hold.")]
    [ProducesResponseType(typeof(UpdateBatchParticipantQuantityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateBatchParticipantQuantity(Guid batchId, [FromBody] UpdateBatchParticipantQuantityRequest request)
    {
        if (request?.NewQuantity <= 0)
            return BadRequest(new { success = false, error = "Quantity must be greater than 0." });

        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var buyerEmail = User.FindFirstValue(ClaimTypes.Email)!;

        var result = await _mediator.Send(new UpdateBatchParticipantQuantityCommand(
            batchId,
            buyerId,
            buyerEmail,
            request.NewQuantity
       ));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/batches/{batchId}/leave
    /// </summary>
    [HttpPost("{batchId:guid}/leave")]
    [EndpointSummary("Buyer leaves a batch and releases the Stripe payment hold.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LeaveBatch(Guid batchId)
    {
        var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _mediator.Send(new LeaveBatchCommand
        {
            BatchId = batchId,
            BuyerId = buyerId
        });

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}

public class JoinBatchRequest
{
    public int Quantity { get; set; }
}

public class ConfirmJoinBatchRequest
{
    public string PaymentIntentId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateBatchParticipantQuantityRequest
{
    public int NewQuantity { get; set; }
}