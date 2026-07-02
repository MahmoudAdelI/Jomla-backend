using FluentValidation;
using Jomla.Application.Common.Extensions;

namespace Jomla.Application.Features.Offers.Commands.UpdateOffer
{
    public sealed class UpdateOfferCommandValidator : AbstractValidator<UpdateOfferCommand>
    {
        public UpdateOfferCommandValidator()
        {
            // Id
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Offer ID is required.");

            // Title
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(255).WithMessage("Title must not exceed 255 characters.");

            // Description
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 2000 characters.")
                .When(x => x.Description is not null);
            // CategoryId
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category is required.");

            // VariantAttributes
            RuleFor(x => x.VariantAttributes)
                .MaximumLength(1000).WithMessage("Variant attributes must not exceed 1000 characters.")
                .When(x => x.VariantAttributes is not null);

            // UnitPrice
            RuleFor(x => x.UnitPrice)
                .GreaterThan(0).WithMessage("Unit price must be greater than zero.");

            // DiscountPercentage
            RuleFor(x => x.DiscountPercentage)
                .InclusiveBetween(0, 100).WithMessage("Discount percentage must be between 0 and 100.");
            // BatchTargetQuantity
            RuleFor(x => x.BatchTargetQuantity)
                .GreaterThan(0).WithMessage("Batch target quantity must be greater than zero.");

            // TotalQuantityAvailable
            RuleFor(x => x.TotalQuantityAvailable)
                .GreaterThan(0).WithMessage("Total quantity available must be greater than zero.")
                .GreaterThanOrEqualTo(x => x.BatchTargetQuantity)
                    .WithMessage("Total quantity available must be at least equal to the batch target quantity.");

            // MinFallbackQuantity
            RuleFor(x => x.MinFallbackQuantity)
                .GreaterThan(0).WithMessage("Minimum fallback quantity must be greater than zero.")
                .LessThanOrEqualTo(x => x.BatchTargetQuantity)
                    .WithMessage("Minimum fallback quantity must not exceed the batch target quantity.")
                .When(x => x.MinFallbackQuantity.HasValue);

            // ExpiresAt
            RuleFor(x => x.ExpiresAt)
                .GreaterThan(DateTime.UtcNow).WithMessage("Expiry date must be in the future.")
                .When(x => x.ExpiresAt.HasValue);

            // Images
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
