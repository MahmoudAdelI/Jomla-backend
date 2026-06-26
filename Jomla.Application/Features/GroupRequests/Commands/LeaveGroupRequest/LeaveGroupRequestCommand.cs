using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.LeaveGroupRequest
{
    public sealed record LeaveGroupRequestCommand(
    Guid GroupRequestId,
    Guid BuyerId
    ) : IRequest<LeaveGroupRequestResponse>;

    public sealed record LeaveGroupRequestResponse(
        bool Success,
        string? Error
    );

}
