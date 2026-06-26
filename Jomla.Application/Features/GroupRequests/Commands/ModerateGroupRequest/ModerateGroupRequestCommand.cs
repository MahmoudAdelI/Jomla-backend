using MediatR;

namespace Jomla.Application.Features.GroupRequests.Commands.ModerateGroupRequest
{
    public sealed record ModerateGroupRequestCommand(Guid GroupRequestId) : IRequest;
}
