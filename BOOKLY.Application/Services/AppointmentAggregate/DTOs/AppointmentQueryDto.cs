namespace BOOKLY.Application.Services.AppointmentAggregate.DTOs
{
    public sealed record AppointmentQueryDto
    {
        public int? OwnerId { get; init; }
        public int? ServiceId { get; init; }
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
        public string? Status { get; init; }
        public string? ClientSearch { get; init; }
        public string? ClientEmail { get; init; }
    }
}
