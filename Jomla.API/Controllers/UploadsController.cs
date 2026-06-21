using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Jomla.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jomla.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]

    //MOW Controllers presents :: Uploads Controller (sound effects with drums)
  
    public class UploadsController : ControllerBase
    {
        private readonly IImageService _imageService;

        public UploadsController(IImageService imageService)
        {
            _imageService = imageService;
        }


        [AllowAnonymous]
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file is null)
                return BadRequest("Image is required.");

            var imageUrl = await _imageService.UploadImageAsync(
                file,
                cancellationToken);

            return Ok(new
            {
                Url = imageUrl
            });
        }




    }
}
