using Jomla.Application.Features.Notifications.DTOs;
using MediatR;

namespace Jomla.Application.Features.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(
    Guid UserId,
    bool? UnreadOnly,
    int Page,
    int PageSize
) : IRequest<GetNotificationsResult>;
