using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.ServiceTypeAggregate.DTOs
{
    public sealed record UpdateServiceTypeDto
    {
        public string? Name { get; init; }
        public string? Description { get; init; }

        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe tener formato HEX #RRGGBB")]
        public string? ColorHex { get; init; }

        [StringLength(50, ErrorMessage = "La key del icono no puede exceder 50 caracteres")]
        public string? IconKey { get; init; }
    }
}
