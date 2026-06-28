using MediatR;
using System;

namespace Jomla.Application.Features.GroupRequests.Commands.ReactivateGroupRequest
{
    public sealed record ReactivateGroupRequestCommand(Guid GroupRequestId) : IRequest;
}
