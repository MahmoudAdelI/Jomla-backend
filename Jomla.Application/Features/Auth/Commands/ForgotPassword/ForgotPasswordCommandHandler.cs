using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.JobDispatcher;
using MediatR;

namespace Jomla.Application.Features.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler(
        IIdentityService _identityService,
        IBackgroundJobDispatcher _jobDispatcher
        ) : IRequestHandler<ForgotPasswordCommand>
    {
        public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _identityService.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // Silently succeed to prevent email enumeration / security issues
                return;
            }

            var token = await _identityService.GeneratePasswordResetTokenAsync(user);

            var subject = "Reset Your Password";
            var body = $@"
                <h3>Reset Password Request</h3>
                <p>Hello {user.FirstName},</p>
                <p>You requested a password reset. Please use the following code to reset your password:</p>
                <p><strong style='font-size: 16px; letter-spacing: 1px; background: #f4f4f4; padding: 8px 12px; display: inline-block;'>{token}</strong></p>
                <p>If you did not request a password reset, please ignore this email.</p>
            ";

            _jobDispatcher.Enqueue<IEmailService>(x => x.SendEmailAsync(user.Email!, subject, body));
        }
    }
}
