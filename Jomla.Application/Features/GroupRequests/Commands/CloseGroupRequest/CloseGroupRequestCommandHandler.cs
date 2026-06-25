using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.GroupRequests.Commands.CloseGroupRequest
{
    public class CloseGroupRequestCommandHandler(IAppDbContext db) : IRequestHandler<CloseGroupRequestCommand>
    {
        private readonly IAppDbContext _db = db;

        public async Task Handle(CloseGroupRequestCommand request, CancellationToken cancellationToken)
        {
            var groupRequest = await _db.GroupRequests
                .FirstOrDefaultAsync(gr => gr.Id == request.GroupRequestId, cancellationToken);
            if (groupRequest is null || groupRequest.Status != GroupRequestStatus.Closed)
                return;

            groupRequest.Status = GroupRequestStatus.Closed;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
