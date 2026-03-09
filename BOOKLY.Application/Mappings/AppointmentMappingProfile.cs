using AutoMapper;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;

namespace BOOKLY.Application.Mappings
{
    public class AppointmentMappingProfile : Profile
    {
        public AppointmentMappingProfile()
        {
            CreateMap<AppointmentStatus, string>()
                .ConvertUsing(s => s.ToString());

            // Appointment → AppointmentDto
            CreateMap<Appointment, AppointmentDto>()
                .ForMember(d => d.ClientName,
                    opt => opt.MapFrom(s => s.Client.ClientName))
                .ForMember(d => d.ClientPhone,
                    opt => opt.MapFrom(s => s.Client.Phone))
                .ForMember(d => d.ClientEmail,
                    opt => opt.MapFrom(s => s.Client.Email.Value))
                .ForMember(d => d.DurationMinutes,
                    opt => opt.MapFrom(s => s.Duration.Value))
                .ForMember(d => d.Status,
                    opt => opt.MapFrom(s => s.Status))
                .ForMember(d => d.FieldValues,
                    opt => opt.MapFrom(s => s.FieldValues));

            // Appointment → AppointmentSummaryDto
            CreateMap<Appointment, AppointmentSummaryDto>()
                .ForMember(d => d.ClientName,
                    opt => opt.MapFrom(s => s.Client.ClientName))
                .ForMember(d => d.Status,
                    opt => opt.MapFrom(s => s.Status));

            // AppointmentFieldValue → AppointmentFieldValueDto
            CreateMap<AppointmentFieldValue, AppointmentFieldValueDto>();
        }
    }
}
