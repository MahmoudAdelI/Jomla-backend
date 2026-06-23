using MediatR;

namespace Jomla.Application.Features.GroupRequests.Commands.CloseGroupRequest
{
    public sealed record CloseGroupRequestCommand(Guid GroupRequestId) : IRequest;
}
