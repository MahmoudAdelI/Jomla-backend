using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MediatR;

namespace Jomla.Application.Features.Users.Commands.UpdateProfileImage
{
    public record UpdateProfileImageCommand : IRequest<string>
    {
        public required Guid UserId { get; init; }
        public required IFormFile File { get; init; }
    }
}
