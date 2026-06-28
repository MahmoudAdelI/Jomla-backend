using FluentValidation;

namespace Jomla.Application.Features.GroupRequests.Commands.RejectGroupRequestOffer
{
    public class RejectGroupRequestOfferCommandValidator : AbstractValidator<RejectGroupRequestOfferCommand>
    {
        public RejectGroupRequestOfferCommandValidator()
        {
            RuleFor(v => v.OfferId)
                .NotEmpty().WithMessage("OfferId is required.");

            RuleFor(v => v.BuyerId)
                .NotEmpty().WithMessage("BuyerId is required.");
        }
    }
}
