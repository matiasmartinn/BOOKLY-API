using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    public sealed record AddUnavailabilityRangeDto
    {
        [Required]
        public DateOnly StartDate { get; init; }

        [Required]
        public DateOnly EndDate { get; init; }

        public TimeOnly? Start { get; init; }
        public TimeOnly? End { get; init; }

        [StringLength(250, ErrorMessage = "La razón no puede exceder 250 caracteres")]
        public string? Reason { get; init; }
    }
}
