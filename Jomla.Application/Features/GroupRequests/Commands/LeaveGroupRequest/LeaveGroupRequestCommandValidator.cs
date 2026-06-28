using FluentValidation;
using System;

namespace Jomla.Application.Features.GroupRequests.Commands.LeaveGroupRequest
{
    public sealed class LeaveGroupRequestCommandValidator : AbstractValidator<LeaveGroupRequestCommand>
    {
        public LeaveGroupRequestCommandValidator()
        {
            RuleFor(x => x.GroupRequestId)
                .NotEmpty().WithMessage("GroupRequestId is required.");

            RuleFor(x => x.BuyerId)
                .NotEmpty().WithMessage("BuyerId is required.");
        }
    }
}
