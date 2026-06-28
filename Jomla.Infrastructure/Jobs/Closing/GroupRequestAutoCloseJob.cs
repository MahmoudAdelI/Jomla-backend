using Jomla.Application.Features.GroupRequests.Commands.CloseGroupRequest;
using Jomla.Application.Jobs.Closing;
using MediatR;

namespace Jomla.Infrastructure.Jobs.Closing
{
    public class GroupRequestAutoCloseJob(ISender sender) : IGroupRequestAutoCloseJob
    {
        private readonly ISender _sender = sender;
        public async Task ExecuteAsync(Guid groupRequestId)
        {
            await _sender.Send(new CloseGroupRequestCommand(groupRequestId));
        }
    }
}
