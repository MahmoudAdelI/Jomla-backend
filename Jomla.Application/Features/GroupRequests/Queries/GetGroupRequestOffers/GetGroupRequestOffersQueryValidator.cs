using FluentValidation;

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOffers;

public sealed class GetGroupRequestOffersQueryValidator
    : AbstractValidator<GetGroupRequestOffersQuery>
{
    public GetGroupRequestOffersQueryValidator()
    {
        RuleFor(x => x.GroupRequestId).NotEmpty()
            .WithMessage("Group request id is required.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page size must be greater than or equal to 1.");
    }
}