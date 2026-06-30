using FluentValidation;
using FluentValidation.Results;
using Jomla.Application.Common.Interfaces;
using MediatR;

namespace Jomla.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler(
        IIdentityService _identityService
        ) : IRequestHandler<ResetPasswordCommand>
    {
        public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _identityService.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // Silently return to prevent user/email enumeration
                return;
            }

            var (succeeded, errors) = await _identityService.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!succeeded)
            {
                var failures = errors.Select(err => new ValidationFailure(string.Empty, err));
                throw new ValidationException(failures);
            }
        }
    }
}
