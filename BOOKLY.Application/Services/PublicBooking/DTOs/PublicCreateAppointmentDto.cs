using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.PublicBooking.DTOs
{
    public sealed record PublicCreateAppointmentDto
    {
        [Required]
        [MaxLength(200)]
        public string ClientName { get; init; } = null!;

        [Required]
        [MaxLength(50)]
        public string ClientPhone { get; init; } = null!;

        [EmailAddress]
        [Required]
        [MaxLength(255)]
        public string ClientEmail { get; init; } = null!;

        [Required]
        public DateTime StartDateTime { get; init; }

        [MaxLength(1000)]
        public string? ClientNotes { get; init; }

        public List<PublicCreateAppointmentFieldValueDto> FieldValues { get; init; } = [];
    }

    public sealed record PublicCreateAppointmentFieldValueDto
    {
        [Required]
        public int FieldDefinitionId { get; init; }

        [Required]
        public string Value { get; init; } = null!;
    }
}
