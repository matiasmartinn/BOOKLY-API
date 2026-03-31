namespace BOOKLY.Application.Services.AppointmentAggregate.DTOs
{
    public sealed record AppointmentStatusHistoryDto
    {
        public int Id { get; init; }
        public int AppointmentId { get; init; }
        public int? UserId { get; init; }
        public string? UserDisplayName { get; init; }
        public string? OldStatus { get; init; }
        public string NewStatus { get; init; } = null!;
        public string? Reason { get; init; }
        public DateTime OccurredOn { get; init; }
    }
}
