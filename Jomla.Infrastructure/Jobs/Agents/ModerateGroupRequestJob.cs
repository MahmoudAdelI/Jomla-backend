using Jomla.Application.Features.GroupRequests.Commands.ModerateGroupRequest;
using Jomla.Application.Jobs.Agents;
using MediatR;

namespace Jomla.Infrastructure.Jobs.Agents
{
    public class ModerateGroupRequestJob(ISender sender) : IModerateGroupRequestJob
    {
        private readonly ISender _sender = sender;

        public async Task ExecuteAsync(Guid groupRequestId, CancellationToken ct)
        {
            await _sender.Send(new ModerateGroupRequestCommand(groupRequestId), ct);
        }
    }
}
