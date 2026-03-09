using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.AppointmentAggregate.DTOs
{
    public sealed record UpdateAppointmentDto
    {
        [MaxLength(200)]
        public string? ClientName { get; init; }

        [MaxLength(50)]
        public string? ClientPhone { get; init; }

        [EmailAddress]
        [MaxLength(255)]
        public string? ClientEmail { get; init; }

        [MaxLength(1000)]
        public string? InternalNotes { get; init; }
    }
}
