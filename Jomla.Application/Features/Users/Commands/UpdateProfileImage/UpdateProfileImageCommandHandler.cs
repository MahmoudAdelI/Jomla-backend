using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Users.Commands.UpdateProfileImage
{
    public class UpdateProfileImageCommandHandler(
       UserManager<AppUser> userManager,
       IImageService imageService)
       : IRequestHandler<UpdateProfileImageCommand, string>
    {
        public async Task<string> Handle(
            UpdateProfileImageCommand request,
            CancellationToken cancellationToken)
        {
            // Step 1: Fetch the user
            var user = await userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                throw new NotFoundException(nameof(AppUser), request.UserId);

            // Step 2: Upload the new image (reuses the same service as UploadsController)
            var imageUrl = await imageService.UploadImageAsync(request.File, cancellationToken);

            // Step 3: Save the new URL on the user
            user.ImageUrl = imageUrl;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new BadRequestException(string.Join(", ", updateResult.Errors.Select(e => e.Description)));

            // Step 4: Return the new URL to the frontend
            return imageUrl;
        }
    }
}
