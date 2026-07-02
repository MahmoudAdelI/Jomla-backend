using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Users.Commands.ChangePassword
{
    public record ChangePasswordCommand : IRequest
    {
        public required Guid UserId { get; init; }
        public required string CurrentPassword { get; init; }
        public required string NewPassword { get; init; }
    }
}
