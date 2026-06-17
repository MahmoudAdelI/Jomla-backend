namespace Jomla.Application.Features.Notifications.DTOs
{
    public sealed record NotificationDto(
        Guid Id,
        string Type,
        string Title,
        string Body,
        Guid? EntityId,
        string? EntityType
    );
}
