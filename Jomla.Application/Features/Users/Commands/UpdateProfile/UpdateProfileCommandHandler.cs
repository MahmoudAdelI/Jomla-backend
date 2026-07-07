using Jomla.Application.Common.Exceptions;
using Jomla.Application.Features.Users.DTOs;
using Jomla.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Users.Commands.UpdateProfile
{

    public class UpdateProfileCommandHandler(UserManager<AppUser> userManager)
        : IRequestHandler<UpdateProfileCommand, UserProfileDto>
    {
        public async Task<UserProfileDto> Handle(
            UpdateProfileCommand request,
            CancellationToken cancellationToken)
        {
            // Step 1: Fetch the user by ID from the token
            var user = await userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                throw new NotFoundException(nameof(AppUser), request.UserId);

            // Step 2: If the email is changing, make sure it's not taken by someone else
            if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await userManager.FindByEmailAsync(request.Email);
                if (existingUser is not null && existingUser.Id != user.Id)
                    throw new ConflictException("Email is already in use.");

                // Step 3: Update email + username through Identity (keeps normalized fields in sync)
                var emailResult = await userManager.SetEmailAsync(user, request.Email);
                if (!emailResult.Succeeded)
                    throw new BadRequestException(string.Join(", ", emailResult.Errors.Select(e => e.Description)));

                var usernameResult = await userManager.SetUserNameAsync(user, request.Email);
                if (!usernameResult.Succeeded)
                    throw new BadRequestException(string.Join(", ", usernameResult.Errors.Select(e => e.Description)));
            }

            // Step 4: Update the simple fields
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            var isBuyer = await userManager.IsInRoleAsync(user, "Buyer");
            if (isBuyer && (!string.IsNullOrEmpty(request.ShippingAddress) || !string.IsNullOrEmpty(request.PhoneNumber)))
            {
                if (user.ContactInfo == null)
                {
                    user.ContactInfo = new UserContactInfo();
                }
                user.ContactInfo.ShippingAddress = request.ShippingAddress ?? string.Empty;
                user.ContactInfo.PhoneNumber = request.PhoneNumber ?? string.Empty;
            }
            else
            {
                user.ContactInfo = null;
            }

            // Step 5: Persist changes
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new BadRequestException(string.Join(", ", updateResult.Errors.Select(e => e.Description)));

            // Step 6: Return the updated profile
            return new UserProfileDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                ImageUrl = user.ImageUrl,
                ShippingAddress = user.ContactInfo?.ShippingAddress,
                PhoneNumber = user.ContactInfo?.PhoneNumber
            };
        }

    }
}
