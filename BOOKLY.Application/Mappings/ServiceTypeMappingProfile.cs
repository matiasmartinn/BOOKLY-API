using AutoMapper;
using BOOKLY.Application.Services.ServiceTypeAggregate.DTOs;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using Microsoft.VisualBasic.FileIO;

namespace BOOKLY.Application.Mappings
{
    public sealed class ServiceTypeMappingProfile : Profile
    {
        public ServiceTypeMappingProfile()
        {
            // ServiceType → ServiceTypeDto
            CreateMap<ServiceType, ServiceTypeDto>()
                .ForMember(d => d.FieldDefinitions, o => o.MapFrom(s => s.FieldDefinitions));

            // ServiceTypeFieldDefinition → ServiceTypeFieldDefinitionDto
            CreateMap<ServiceTypeFieldDefinition, ServiceTypeFieldDefinitionDto>()
                .ForMember(d => d.Key, o => o.MapFrom(s => s.Key.Value))
                .ForMember(d => d.Label, o => o.MapFrom(s => s.Label.Value))
                .ForMember(d => d.FieldType, o => o.MapFrom(s => (int)s.FieldType))
                .ForMember(d => d.Options, o => o.MapFrom(s => s.Options));

            // ServiceTypeFieldOption → ServiceTypeFieldOptionDto
            CreateMap<ServiceTypeFieldOption, ServiceTypeFieldOptionDto>();
        }
    }
}
