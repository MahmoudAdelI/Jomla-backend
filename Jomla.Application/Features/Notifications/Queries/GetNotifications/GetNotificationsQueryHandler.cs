using AutoMapper;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jomla.Application.Features.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(
    IAppDbContext db,
    IMapper mapper
) : IRequestHandler<GetNotificationsQuery, GetNotificationsResult>
{
    private readonly IAppDbContext _db = db;
    private readonly IMapper _mapper = mapper;

    public async Task<GetNotificationsResult> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var baseQuery = _db.Notifications
            .Where(n => n.UserId == request.UserId);

        // Count unread before applying unread-only filter
        var unreadCount = await baseQuery
            .CountAsync(n => !n.IsRead, cancellationToken);

        if (request.UnreadOnly == true)
            baseQuery = baseQuery.Where(n => !n.IsRead);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => _mapper.Map<NotificationDto>(n))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new GetNotificationsResult(
            items,
            totalCount,
            unreadCount,
            request.Page,
            request.PageSize,
            totalPages
        );
    }
}
