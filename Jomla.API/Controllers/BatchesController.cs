using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Batches.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jomla.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BatchesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IIdentityService _identityService;  // ← استخدم IIdentityService

        public BatchesController(IMediator mediator, IIdentityService identityService)
        {
            _mediator = mediator;
            _identityService = identityService;
        }

        /// <summary>
        /// POST /api/batches/{batchId}/join
        /// Buyer joins a batch
        /// </summary>
        [HttpPost("{batchId}/join")]
        public async Task<IActionResult> JoinBatch(Guid batchId, [FromBody] JoinBatchRequest request)
        {
            if (request?.Quantity <= 0)
                return BadRequest(new { success = false, error = "Quantity must be greater than 0." });

            var buyerId = _identityService.GetCurrentUserId();  // ← استخدم الـ service
            var buyerEmail = _identityService.GetCurrentUserEmail();  // ← استخدم الـ service

            if (buyerId == Guid.Empty)
                return Unauthorized();

            var command = new JoinBatchCommand
            {
                BatchId = batchId,
                BuyerId = buyerId,
                BuyerEmail = buyerEmail,
                Quantity = request.Quantity
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                if (result.StatusCode.HasValue)
                    return StatusCode(result.StatusCode.Value, result);

                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// DELETE /api/batches/{batchId}/leave
        /// Buyer leaves a batch
        /// </summary>
        [HttpDelete("{batchId}/leave")]
        public async Task<IActionResult> LeaveBatch(Guid batchId)
        {
            var buyerId = _identityService.GetCurrentUserId();  // ← استخدم الـ service

            if (buyerId == Guid.Empty)
                return Unauthorized();

            var command = new LeaveBatchCommand
            {
                BatchId = batchId,
                BuyerId = buyerId
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }

    public class JoinBatchRequest
    {
        public int Quantity { get; set; }
    }
}