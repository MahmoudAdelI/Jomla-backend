using MediatR;

namespace Jomla.Application.Features.Notifications.Commands.MarkAsRead;

public sealed record MarkAsReadCommand(Guid NotificationId, Guid UserId) : IRequest<MarkAsReadResult>;
