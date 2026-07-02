using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Users.Commands.UpdateProfileImage
{
    public class UpdateProfileImageCommandValidator : AbstractValidator<UpdateProfileImageCommand>
    {
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public UpdateProfileImageCommandValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("Image file is required.");

            RuleFor(x => x.File)
                .Must(f => f.Length > 0)
                .WithMessage("Image file cannot be empty.")
                .Must(f => f.Length <= MaxFileSizeBytes)
                .WithMessage("Image size must not exceed 5 MB.")
                .Must(f => AllowedExtensions.Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
                .WithMessage("Only .jpg, .jpeg, .png, and .webp files are allowed.");
        }
    }
}
