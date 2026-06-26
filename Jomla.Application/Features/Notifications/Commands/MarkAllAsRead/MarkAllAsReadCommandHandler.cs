using Jomla.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Notifications.Commands.MarkAllAsRead;

public sealed class MarkAllAsReadCommandHandler(IAppDbContext db)
    : IRequestHandler<MarkAllAsReadCommand, MarkAllAsReadResult>
{
    private readonly IAppDbContext _db = db;

    public async Task<MarkAllAsReadResult> Handle(
        MarkAllAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == request.UserId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
            return new MarkAllAsReadResult(0);

        foreach (var notification in unread)
            notification.IsRead = true;

        await _db.SaveChangesAsync(cancellationToken);

        return new MarkAllAsReadResult(unread.Count);
    }
}
