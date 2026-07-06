using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Commands.CreateAdmin
{
    public sealed record CreateAdminCommand(string Email, string Password, string FirstName, string LastName) : IRequest;
}
