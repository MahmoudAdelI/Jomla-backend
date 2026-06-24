using MediatR;

namespace Jomla.Application.Features.GroupRequests.Commands.MatchSuppliersForGroupRequest
{
    public sealed record MatchSuppliersForGroupRequestCommand(
        Guid GroupRequestId,
        Guid CategoryId,
        int CurrentQuantity
        ) : IRequest;
}
