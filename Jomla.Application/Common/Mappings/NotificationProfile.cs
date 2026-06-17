using AutoMapper;
using Jomla.Application.Features.Notifications.DTOs;
using Jomla.Domain.Entities;

namespace Jomla.Application.Common.Mappings
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            CreateMap<Notification, NotificationDto>();
        }
    }
}
