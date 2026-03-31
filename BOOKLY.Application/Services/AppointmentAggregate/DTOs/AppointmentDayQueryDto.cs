using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.AppointmentAggregate.DTOs
{
    public sealed record AppointmentDayQueryDto
    {
        [Required]
        public DateOnly Date { get; init; }

        public int? OwnerId { get; init; }

        public int? ServiceId { get; init; }
    }
}
