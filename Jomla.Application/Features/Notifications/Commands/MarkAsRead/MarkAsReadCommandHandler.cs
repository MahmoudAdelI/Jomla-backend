using Jomla.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Notifications.Commands.MarkAsRead;

public sealed class MarkAsReadCommandHandler(IAppDbContext db)
    : IRequestHandler<MarkAsReadCommand, MarkAsReadResult>
{
    private readonly IAppDbContext _db = db;

    public async Task<MarkAsReadResult> Handle(
        MarkAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == request.NotificationId && n.UserId == request.UserId,
                cancellationToken);

        if (notification is null)
            return new MarkAsReadResult(false, "Notification not found.");

        if (notification.IsRead)
            return new MarkAsReadResult(true); // idempotent — already read

        notification.IsRead = true;

        await _db.SaveChangesAsync(cancellationToken);

        return new MarkAsReadResult(true);
    }
}
