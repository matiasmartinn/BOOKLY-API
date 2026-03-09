using AutoMapper;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;

namespace BOOKLY.Application.Mappings
{
    internal class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(d => d.Email, o => o.MapFrom(s => s.Email.Value))
                .ForMember(d => d.Role, o => o.MapFrom(s => s.Role));
        }
    }
}
