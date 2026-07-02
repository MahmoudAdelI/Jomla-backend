using Jomla.Application.Features.Users.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Users.Commands.UpdateProfile
{
    public record UpdateProfileCommand : IRequest<UserProfileDto>
    {
        public required Guid UserId { get; init; }
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
        public required string Email { get; init; }
    }
}
