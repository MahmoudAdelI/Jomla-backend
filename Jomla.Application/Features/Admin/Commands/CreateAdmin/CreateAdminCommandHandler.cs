using FluentValidation;
using FluentValidation.Results;
using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Commands.CreateAdmin
{
    public sealed class CreateAdminCommandHandler : IRequestHandler<CreateAdminCommand>
    {
        private readonly IIdentityService _identityService;

        public CreateAdminCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task Handle(CreateAdminCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _identityService.FindByEmailAsync(request.Email);
            if (existingUser != null)
                throw new ConflictException("Email already registered.");

            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var (succeeded, errors) = await _identityService.CreateUserAsync(user, request.Password, UserRole.Admin);
            if (!succeeded)
            {
                var failures = errors.Select(e => new ValidationFailure(string.Empty, e));

                throw new ValidationException(failures);
            }
                
        }
    }
}
