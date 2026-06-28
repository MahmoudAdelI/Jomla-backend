using FluentValidation;
using System;

namespace Jomla.Application.Features.GroupRequests.Commands.JoinGroupRequest
{
    public sealed class JoinGroupRequestCommandValidator : AbstractValidator<JoinGroupRequestCommand>
    {
        public JoinGroupRequestCommandValidator()
        {
            RuleFor(x => x.GroupRequestId)
                .NotEmpty().WithMessage("GroupRequestId is required.");

            RuleFor(x => x.BuyerId)
                .NotEmpty().WithMessage("BuyerId is required.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        }
    }
}
