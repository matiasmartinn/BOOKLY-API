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
                .ForMember(d => d.CancelReason,
                    opt => opt.MapFrom(s => s.CancelReason))
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

            CreateMap<AppointmentStatusHistory, AppointmentStatusHistoryDto>()
                .ForMember(d => d.UserDisplayName,
                    opt => opt.MapFrom(s => s.User == null
                        ? null
                        : $"{s.User.PersonName.FirstName} {s.User.PersonName.LastName}"))
                .ForMember(d => d.OldStatus,
                    opt => opt.MapFrom(s => s.OldStatus.HasValue ? s.OldStatus.Value.ToString() : null))
                .ForMember(d => d.NewStatus,
                    opt => opt.MapFrom(s => s.NewStatus.ToString()));
        }
    }
}
