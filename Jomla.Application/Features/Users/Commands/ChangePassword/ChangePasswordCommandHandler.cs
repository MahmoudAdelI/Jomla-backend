using Jomla.Application.Common.Exceptions;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Users.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler(UserManager<AppUser> userManager)
      : IRequestHandler<ChangePasswordCommand>
    {
        public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            // Step 1: Fetch the user
            var user = await userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                throw new NotFoundException(nameof(AppUser), request.UserId);

            // Step 2: Let Identity verify the current password and hash the new one
            var result = await userManager.ChangePasswordAsync(
                user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
                throw new BadRequestException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
