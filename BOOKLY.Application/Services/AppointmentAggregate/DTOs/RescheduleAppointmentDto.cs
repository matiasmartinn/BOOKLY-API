using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.AppointmentAggregate.DTOs
{
    public sealed record RescheduleAppointmentDto
    {
        [Required]
        public DateTime StartDateTime { get; init; }
    }
}
