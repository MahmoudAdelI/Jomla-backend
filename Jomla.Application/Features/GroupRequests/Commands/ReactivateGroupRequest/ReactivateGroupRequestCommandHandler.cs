using Jomla.Application.Common.Interfaces;
using Jomla.Application.Jobs.JobDispatcher;
using Jomla.Application.Jobs.Matching;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jomla.Application.Features.GroupRequests.Commands.ReactivateGroupRequest
{
    public sealed class ReactivateGroupRequestCommandHandler : IRequestHandler<ReactivateGroupRequestCommand>
    {
        private readonly IAppDbContext _context;
        private readonly IBackgroundJobDispatcher _jobDispatcher;

        public ReactivateGroupRequestCommandHandler(
            IAppDbContext context,
            IBackgroundJobDispatcher jobDispatcher)
        {
            _context = context;
            _jobDispatcher = jobDispatcher;
        }

        public async Task Handle(ReactivateGroupRequestCommand request, CancellationToken cancellationToken)
        {
            var groupRequest = await _context.GetGroupRequestWithLockAsync(request.GroupRequestId, cancellationToken);
            if (groupRequest == null || groupRequest.Status != GroupRequestStatus.Inactive)
                return;

            groupRequest.Status = GroupRequestStatus.Active;
            groupRequest.InactiveSince = null;

            await _context.SaveChangesAsync(cancellationToken);

            _jobDispatcher.Enqueue<ISupplierMatchingJob>(j =>
                j.ExecuteAsync(groupRequest.Id, groupRequest.CategoryId, groupRequest.CurrentQuantity));
        }
    }
}
