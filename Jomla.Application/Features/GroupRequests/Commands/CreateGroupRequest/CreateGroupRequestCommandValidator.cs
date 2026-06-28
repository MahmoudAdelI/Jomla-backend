using FluentValidation;
using Jomla.Application.Features.Offers.Commands.CreateOffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.CreateGroupRequest
{
    public sealed class CreateGroupRequestCommandValidator : AbstractValidator<CreateGroupRequestCommand>
    {
        public CreateGroupRequestCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

            RuleFor(x => x.Images)
                .Must(images => images!.Count <= 10).WithMessage("You may upload up to 10 images.")
                .When(x => x.Images is not null);

            RuleForEach(x => x.Images)
                .Must(file => file.Length <= 5 * 1024 * 1024).WithMessage("Each image must not exceed 5 MB.")
                .Must(file => new[] { "image/jpeg", "image/png", "image/webp" }.Contains(file.ContentType))
                    .WithMessage("Only JPEG, PNG, and WebP images are accepted.")
                .When(x => x.Images is not null);
        }
    }
}
