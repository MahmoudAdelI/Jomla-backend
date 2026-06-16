using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Jomla.Application.Common.Exceptions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Auth.DTOs;
using Jomla.Domain.Entities;
using MediatR;

namespace Jomla.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler(
        IIdentityService _identityService,
        ITokenService _tokenService,
        IMapper _mapper
        ) : IRequestHandler<RegisterCommand, AuthResponseDto>
    {
        public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (await _identityService.FindByEmailAsync(request.Email) != null)
                throw new ConflictException("Email already registered");

            var user = _mapper.Map<AppUser>(request);

            var (succeeded, errors) = await _identityService.CreateUserAsync(user, request.Password, request.Role);

            if (!succeeded)
            {
                var failures = errors.Select(err => new ValidationFailure(string.Empty, err));
                throw new ValidationException(failures);
            }
            
            var roles = await _identityService.GetUserRolesAsync(user);

            var token = _tokenService.GenerateToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshTokens.Add(refreshToken);
            await _identityService.UpdateUserAsync(user);

            return new AuthResponseDto(
                token,
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                refreshToken.Token,
                refreshToken.ExpiresOn
            );
        }
    }
}
