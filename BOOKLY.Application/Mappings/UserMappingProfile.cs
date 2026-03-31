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
               .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
               .ForMember(d => d.Email, o => o.MapFrom(s => s.Email.Value))
               .ForMember(d => d.FirstName, o => o.MapFrom(s => s.PersonName.FirstName))
               .ForMember(d => d.LastName, o => o.MapFrom(s => s.PersonName.LastName))
               .ForMember(d => d.Role, o => o.MapFrom(s => s.Role))
               .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive))
               .ForMember(d => d.EmailConfirmed, o => o.MapFrom(s => s.EmailConfirmed))
               .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
               .ForMember(d => d.LastLoginAt, o => o.MapFrom(s => s.LastLoginAt));
        }
    }
}
