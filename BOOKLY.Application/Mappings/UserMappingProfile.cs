using AutoMapper;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;

namespace BOOKLY.Application.Mappings
{
    public sealed class UserMappingProfile : Profile
    {
        public const string ServiceIdsContextKey = "User.ServiceIds";

        public UserMappingProfile()
        {
            CreateMap<UserRole, string>()
                .ConvertUsing(role => role.ToString());

            CreateMap<UserStatus, string>()
                .ConvertUsing(status => status.ToString());

            CreateMap<User, UserDto>()
               .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
               .ForMember(d => d.Email, o => o.MapFrom(s => s.Email.Value))
               .ForMember(d => d.FirstName, o => o.MapFrom(s => s.PersonName.FirstName))
               .ForMember(d => d.LastName, o => o.MapFrom(s => s.PersonName.LastName))
               .ForMember(d => d.Role, o => o.MapFrom(s => s.Role))
               .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive))
               .ForMember(d => d.EmailConfirmed, o => o.MapFrom(s => s.EmailConfirmed))
               .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
               .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
               .ForMember(d => d.LastLoginAt, o => o.MapFrom(s => s.LastLoginAt))
               .ForMember(d => d.ServiceIds, o => o.MapFrom((_, _, _, context) => GetServiceIds(context)));

            CreateMap<User, SecretaryDto>()
               .ForMember(d => d.FirstName, o => o.MapFrom(s => s.PersonName.FirstName))
               .ForMember(d => d.LastName, o => o.MapFrom(s => s.PersonName.LastName))
               .ForMember(d => d.Email, o => o.MapFrom(s => s.Email.Value))
               .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
               .ForMember(d => d.ServiceIds, o => o.MapFrom((_, _, _, context) => GetServiceIds(context)));
        }

        private static IReadOnlyCollection<int> GetServiceIds(ResolutionContext context)
        {
            return context.Items.TryGetValue(ServiceIdsContextKey, out var serviceIds) &&
                   serviceIds is IReadOnlyCollection<int> values
                ? values
                : [];
        }
    }
}
