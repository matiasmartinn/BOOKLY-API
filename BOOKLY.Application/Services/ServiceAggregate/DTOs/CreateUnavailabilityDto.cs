using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    public sealed record CreateUnavailabilityDto
    {
        [Required]
        public DateOnly StartDate { get; init; }
        [Required]
        public DateOnly EndDate { get; init; }
        public TimeOnly? StartTime { get; init; }
        public TimeOnly? EndTime { get; init; }
        public string? Reason { get; init; }
    }
}
