using FluentValidation;

namespace Jomla.Application.Features.Batches.Queries.SearchBatches
{
    public sealed class SearchBatchesQueryValidator : AbstractValidator<SearchBatchesQuery>
    {
        public SearchBatchesQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("PageSize must be greater than 0.");
        }
    }
}
