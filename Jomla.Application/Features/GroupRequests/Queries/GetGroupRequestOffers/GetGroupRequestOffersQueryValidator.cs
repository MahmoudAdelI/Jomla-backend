using FluentValidation;

namespace Jomla.Application.Features.GroupRequests.Queries.GetGroupRequestOffers;

public sealed class GetGroupRequestOffersQueryValidator
    : AbstractValidator<GetGroupRequestOffersQuery>
{
    public GetGroupRequestOffersQueryValidator()
    {
        RuleFor(x => x.GroupRequestId).NotEmpty()
            .WithMessage("Group request id is required.");
    }
}