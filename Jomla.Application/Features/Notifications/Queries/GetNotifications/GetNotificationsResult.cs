using Jomla.Application.Features.Notifications.DTOs;

namespace Jomla.Application.Features.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsResult(
    List<NotificationDto> Items,
    int TotalCount,
    int UnreadCount,
    int Page,
    int PageSize,
    int TotalPages
);
