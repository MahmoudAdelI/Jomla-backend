using AutoMapper;
using Jomla.Application.Features.Auth.Commands.Register;
using Jomla.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Common.Mappings
{
    public class AuthProfile : Profile  
    {
        public AuthProfile()
        {
            CreateMap<RegisterCommand, AppUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
        }
    }
}
