using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.AppointmentAggregate.DTOs
{
    public sealed record CreateAppointmentDto
    {
        [Required]
        public int ServiceId { get; init; }

        public int? UserId { get; init; }

        public int? AssignedSecretaryId { get; init; }

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

        public List<CreateAppointmentFieldValueDto> FieldValues { get; init; } = [];
    }
    public sealed record CreateAppointmentFieldValueDto
    {
        [Required]
        public int FieldDefinitionId { get; init; }

        [Required]
        public string Value { get; init; } = null!;
    }


}
