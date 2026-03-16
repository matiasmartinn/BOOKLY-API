namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    public sealed record ScheduleUnavailabilityDto
    {
        public int Id { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
        public TimeOnly? StartTime { get; init; }
        public TimeOnly? EndTime { get; init; }
        public string? Reason { get; init; }

        public bool IsFullDay => StartTime is null && EndTime is null;
    }
}
