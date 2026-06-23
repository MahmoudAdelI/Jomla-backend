using Jomla.Application.Features.GroupRequests.Commands.MatchSuppliersForGroupRequest;
using Jomla.Application.Jobs.Matching;
using MediatR;

namespace Jomla.Infrastructure.Jobs.Matching
{
    public class SupplierMatchingJob(ISender sender) : ISupplierMatchingJob
    {
        private readonly ISender _sender = sender;
        public async Task ExcuteAsync(Guid groupRequestId, Guid categoryId, int currentQuantity)
        {
            var command = new MatchSuppliersForGroupRequestCommand(groupRequestId, categoryId, currentQuantity);
            await _sender.Send(command);
        }
    }
}
