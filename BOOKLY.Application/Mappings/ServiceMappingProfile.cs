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
                .ForMember(d => d.Mode, o => o.MapFrom(s => s.Mode.ToString()))
                .ForMember(d => d.Schedules, o => o.MapFrom(s => s.ServiceSchedules));

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

            // CreateServiceScheduleDto → ServiceSchedule
            CreateMap<CreateServiceScheduleDto, ServiceSchedule>()
                .ConstructUsing(dto => ServiceSchedule.Create(
                    TimeRange.Create(dto.StartTime, dto.EndTime),
                    Capacity.Create(dto.Capacity),
                    Day.Create(dto.Day)
                ));
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
