using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.CreateGroupRequest
{
    public record CreateGroupRequestCommand(
     Guid InitiatorId,
     string Title,
     int Quantity,
     string? Description,
     List<string>? ImageUrls
     ) : IRequest<CreateGroupRequestResponse>;



    public record CreateGroupRequestResponse(
    bool Success,
    Guid? GroupRequestId,
    string? Error
    );
}
