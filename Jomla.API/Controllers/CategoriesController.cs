using Jomla.Application.Features.Categories.Commands.CreateCategory;
using Jomla.Application.Features.Categories.Commands.DeleteCategory;
using Jomla.Application.Features.Categories.Commands.UpdateCategory;
using Jomla.Application.Features.Categories.Queries.GetCategoryById;
using Jomla.Application.Features.Categories.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

//MOW Controllers presents :: Categories Controller (sound effects with drums)
namespace Jomla.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCategoryCommand command)
        {
            var id = await _mediator.Send(command);

            return Ok(new
            {
                Success = true,
                CategoryId = id
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetCategoriesQuery());

            return Ok(result);
        }


        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var category = await _mediator.Send(
                new GetCategoryByIdQuery(id));

            if (category is null)
                return NotFound();

            return Ok(category);
        }


        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id,UpdateCategoryCommand command)
        {
            if (id != command.Id)
                return BadRequest();
            var updated = await _mediator.Send(command);

            if (!updated)
                return NotFound();

            return Ok(new
            {
                Success = true
            });

        }


        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            // I check if the category exists before attempting to delete it, but for simplicity, i wll just attempt the delete and return NotFound if it fails.
            var deleted = await _mediator.Send(
            new DeleteCategoryCommand(id));

            if (!deleted)
                return NotFound();

            return Ok(new
            {
                Success = true
            });

        }

    }
}
