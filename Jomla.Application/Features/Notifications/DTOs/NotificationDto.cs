using Jomla.Domain;

namespace Jomla.Application.Features.Notifications.DTOs
{
    public sealed record NotificationDto(
        Guid Id,
        NotificationType Type,
        string Title,
        string Body,
        Guid? EntityId,
        string? EntityType,
        bool isRead,
        DateTime CreatedAt
    );
}
