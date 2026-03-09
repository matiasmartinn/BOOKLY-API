using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.ServiceTypeAggregate.DTOs
{
    public sealed record AddServiceTypeFieldOptionDto
    {
        [Required(ErrorMessage = "El value es requerido")]
        [StringLength(60, MinimumLength = 1,
            ErrorMessage = "El value debe tener entre 1 y 60 caracteres")]
        public string Value { get; init; } = null!;

        [Required(ErrorMessage = "El label es requerido")]
        [StringLength(80, MinimumLength = 1,
            ErrorMessage = "El label debe tener entre 1 y 80 caracteres")]
        public string Label { get; init; } = null!;

        [Range(0, int.MaxValue, ErrorMessage = "El orden no puede ser negativo")]
        public int SortOrder { get; init; }
    }
}