namespace BOOKLY.Application.Services.MetricsAggregate.DTOs
{
    public sealed record AppointmentMetricsQueryDto
    {
        public int? OwnerId { get; init; }
        public int? ServiceId { get; init; }
        public int? SecretaryId { get; init; }
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
    }
}
