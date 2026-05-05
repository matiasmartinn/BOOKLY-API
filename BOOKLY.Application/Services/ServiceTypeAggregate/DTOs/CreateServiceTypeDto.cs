using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.ServiceTypeAggregate.DTOs
{
    public sealed record CreateServiceTypeDto
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        public string Name { get; init; } = null!;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; init; }

        [Required(ErrorMessage = "El color es requerido")]
        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe tener formato HEX #RRGGBB")]
        public string ColorHex { get; init; } = null!;

        [StringLength(50, ErrorMessage = "La key del icono no puede exceder 50 caracteres")]
        public string? IconKey { get; init; }
    }
}
