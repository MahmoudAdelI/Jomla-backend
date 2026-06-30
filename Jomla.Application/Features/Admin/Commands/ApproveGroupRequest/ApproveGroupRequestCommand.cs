using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Admin.Commands.ApproveGroupRequest
{
    public sealed record ApproveGroupRequestCommand(Guid GroupRequestId) : IRequest;
}
