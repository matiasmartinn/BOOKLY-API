namespace BOOKLY.Application.Services.AppointmentAggregate.DTOs
{
    public sealed record AppointmentDto
    {
        public int Id { get; init; }
        public int ServiceId { get; init; }
        public int? AssignedSecretaryId { get; init; }
        public string ClientName { get; init; } = null!;
        public string ClientPhone { get; init; } = null!;
        public string ClientEmail { get; init; } = null!;
        public DateTime StartDateTime { get; init; }
        public DateTime EndDateTime { get; init; }
        public int DurationMinutes { get; init; }
        public string Status { get; init; } = null!;
        public string? ClientNotes { get; init; }
        public string? InternalNotes { get; init; }
        public DateTime CreatedOn { get; init; }
        public List<AppointmentFieldValueDto> FieldValues { get; init; } = [];
    }

    public sealed record AppointmentFieldValueDto
    {
        public int FieldDefinitionId { get; init; }
        public string Value { get; init; } = null!;
    }
}
