namespace Jomla.Application.Features.Notifications.Commands.MarkAsRead;

public sealed record MarkAsReadResult(bool Success, string? Error = null);
