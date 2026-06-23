using MediatR;

namespace Jomla.Application.Features.GroupRequests.Commands
{
    public sealed record MatchSuppliersForGroupRequestCommand(
        Guid GroupRequestId,
        Guid CategoryId,
        int CurrentQuantity
        ) : IRequest;
}
