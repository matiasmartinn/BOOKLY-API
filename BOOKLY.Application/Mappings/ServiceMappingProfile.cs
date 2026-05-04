using AutoMapper;
using BOOKLY.Application.Services.ServiceAggregate.DTOs;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;

namespace BOOKLY.Application.Mappings
{
    public class ServiceMappingProfile : Profile
    {
        public ServiceMappingProfile()
        {
            // Service → DTOs
            CreateMap<Service, ServiceDto>()
                .ForMember(d => d.Slug, o => o.MapFrom(s => s.Slug.Value))
                .ForMember(d => d.DurationMinutes, o => o.MapFrom(s => s.DurationMinutes.Value))
                .ForMember(d => d.Capacity, o => o.MapFrom(s => s.Capacity.Value))
                .ForMember(d => d.PlaceName, o => o.MapFrom(s => s.Location != null ? s.Location.PlaceName : null))
                .ForMember(d => d.Address, o => o.MapFrom(s => s.Location != null ? s.Location.Address : null))
                .ForMember(d => d.Mode, o => o.MapFrom(s => s.Mode.ToString()))
                .ForMember(d => d.SecretaryPermissions, o => o.MapFrom(s => s.ServiceSecretaries))
                .ForMember(d => d.Schedules, o => o.MapFrom(s => s.ServiceSchedules));

            CreateMap<ServiceSecretary, ServiceSecretaryPermissionsDto>();

            // ServiceSchedule → ServiceScheduleDto
            CreateMap<ServiceSchedule, ServiceScheduleDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.StartTime, o => o.MapFrom(s => s.Range.Start))
                .ForMember(d => d.EndTime, o => o.MapFrom(s => s.Range.End))
                .ForMember(d => d.Capacity, o => o.MapFrom(s => s.Capacity.Value))
                .ForMember(d => d.Day, o => o.MapFrom(s => GetDayName(s.Day.Value)))
                .ForMember(d => d.DayValue, o => o.MapFrom(s => s.Day.Value));

            CreateMap<ServiceUnavailability, ScheduleUnavailabilityDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.StartDate, o => o.MapFrom(s => s.DateRange.Start))
                .ForMember(d => d.EndDate, o => o.MapFrom(s => s.DateRange.End))
                .ForMember(d => d.StartTime, o => o.MapFrom(s => s.TimeRange != null ? s.TimeRange.Start : (TimeOnly?)null))
                .ForMember(d => d.EndTime, o => o.MapFrom(s => s.TimeRange != null ? s.TimeRange.End : (TimeOnly?)null));
        }
        private static string GetDayName(int dayValue) => dayValue switch
        {
            0 => "Domingo",
            1 => "Lunes",
            2 => "Martes",
            3 => "Miércoles",
            4 => "Jueves",
            5 => "Viernes",
            6 => "Sábado",
            _ => "Desconocido"
        };

    }
}
