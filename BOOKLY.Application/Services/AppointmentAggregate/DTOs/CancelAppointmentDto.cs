using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.AppointmentAggregate.DTOs
{
    public sealed record CancelAppointmentDto
    {
        public int? UserId { get; init; }

        [MaxLength(1000)]
        public string? Reason { get; init; }
    }
}
