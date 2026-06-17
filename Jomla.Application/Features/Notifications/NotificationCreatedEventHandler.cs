using AutoMapper;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Features.Notifications.DTOs;
using MediatR;

namespace Jomla.Application.Features.Notifications
{
    public class NotificationCreatedEventHandler(
        IAppDbContext db,
        IRealtimeService realtimeService,
        IMapper mapper
        ) : INotificationHandler<NotificationCreatedEvent>
    {
        private readonly IAppDbContext _db = db;
        private readonly IRealtimeService _realtimeService = realtimeService;
        private readonly IMapper _mapper = mapper;

        public async Task Handle(NotificationCreatedEvent e, CancellationToken cancellationToken)
        {
            var notification = await _db.Notifications
                .FindAsync([e.NotificationId], cancellationToken);
            if (notification is null) return;

            await _realtimeService.SendNotificationAsync(e.UserId, _mapper.Map<NotificationDto>(notification));
        }
    }
}
