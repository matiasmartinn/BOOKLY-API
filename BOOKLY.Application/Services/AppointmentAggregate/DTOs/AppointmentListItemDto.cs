namespace BOOKLY.Application.Services.AppointmentAggregate.DTOs
{
    public sealed record AppointmentListItemDto
    {
        public int Id { get; init; }
        public int ServiceId { get; init; }
        public string ServiceName { get; init; } = null!;
        public int? AssignedSecretaryId { get; init; }
        public string ClientName { get; init; } = null!;
        public string ClientPhone { get; init; } = null!;
        public string ClientEmail { get; init; } = null!;
        public DateTime StartDateTime { get; init; }
        public DateTime EndDateTime { get; init; }
        public int DurationMinutes { get; init; }
        public string Status { get; init; } = null!;
        public string? ClientNotes { get; init; }
        public string? CancelReason { get; init; }
        public DateTime CreatedOn { get; init; }
    }
}
