using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    public sealed record ScheduleUnavailabilityDto
    {
        public int Id { get; init; }
        public DateOnly Date { get; init; }
        public TimeOnly? Start { get; init; }
        public TimeOnly? End { get; init; }
        public string? Reason { get; init; }
    }
}
