using MediatR;

namespace Jomla.Application.Features.Notifications.Commands.MarkAllAsRead;

public sealed record MarkAllAsReadCommand(Guid UserId) : IRequest<MarkAllAsReadResult>;
