using FluentValidation;

namespace Jomla.Application.Features.SupplierCategoryPreferences.Commands.SaveSupplierCategoryPreference;

public class SaveSupplierCategoryPreferenceCommandValidator : AbstractValidator<SaveSupplierCategoryPreferenceCommand>
{
    public SaveSupplierCategoryPreferenceCommandValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty();

        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.MinQuantity)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Minimum quantity must be at least 1.");
    }
}
