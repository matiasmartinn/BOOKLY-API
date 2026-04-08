using AutoMapper;
using BOOKLY.Application.Services.MetricsAggregate.DTOs;
using BOOKLY.Application.Services.MetricsAggregate.Models;
using BOOKLY.Domain.Queries;

namespace BOOKLY.Application.Mappings
{
    public sealed class MetricsMappingProfile : Profile
    {
        public MetricsMappingProfile()
        {
            CreateMap<AppointmentDayCountResult, AppointmentMetricsDayBucketDto>();

            CreateMap<AppointmentHourCountResult, AppointmentMetricsHourBucketDto>()
                .ForMember(d => d.Label, o => o.MapFrom(s => $"{s.Hour:00}:00"));

            CreateMap<AppointmentMetricsWeekdayBucketSource, AppointmentMetricsWeekdayBucketDto>();

            CreateMap<AppointmentMetricsSnapshot, AppointmentMetricsDto>();
        }
    }
}
