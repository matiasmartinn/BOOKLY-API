using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.AppointmentAggregate.DTOs
{
    public sealed record UpdateAppointmentDto
    {
        [Required]
        [MaxLength(200)]
        public string ClientName { get; init; } = null!;
        [Required]

        [MaxLength(50)]
        public string ClientPhone { get; init; } = null !;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string ClientEmail { get; init; } = null!;

        [MaxLength(1000)]
        public string? ClientNotes { get; init; }
    }
}
