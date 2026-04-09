namespace BOOKLY.Application.Services.AppointmentAggregate.DTOs
{
    public record AppointmentSummaryDto
    {
        public int Id { get; init; }
        public DateTime StartDateTime { get; init; }
        public DateTime EndDateTime { get; init; }
        public string ClientName { get; init; } = null!;
        public string ClientPhone { get; init; } = null!;
        public string? ClientNotes { get; init; }
        public string Status { get; init; } = null!;
        public int? AssignedSecretaryId { get; init; }
    }
}
