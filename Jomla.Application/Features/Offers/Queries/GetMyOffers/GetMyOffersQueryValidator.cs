using FluentValidation;
using Jomla.Domain;

namespace Jomla.Application.Features.Offers.Queries.GetMyOffers;

public sealed class GetMyOffersQueryValidator : AbstractValidator<GetMyOffersQuery>
{
    public GetMyOffersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .When(x => x.PageNumber.HasValue);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .When(x => x.PageSize.HasValue);

        RuleFor(x => x)
            .Must(x => x.PageNumber.HasValue == x.PageSize.HasValue)
            .WithMessage("PageNumber and PageSize must be provided together.");

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.CategoryId)
            .NotEqual(Guid.Empty)
            .When(x => x.CategoryId.HasValue);

        RuleFor(x => x.SortBy)
            .IsInEnum()
            .When(x => x.SortBy.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);
    }
}