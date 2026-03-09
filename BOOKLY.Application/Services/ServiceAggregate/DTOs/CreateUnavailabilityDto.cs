using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    public sealed record CreateUnavailabilityDto
    {
        [Required]
        public DateOnly Date { get; init; }
        public TimeOnly? Start { get; init; }
        public TimeOnly? End { get; init; }

        [StringLength(250, ErrorMessage = "La razón no puede exceder 250 caracteres")]
        public string? Reason { get; init; }
    }
}
