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
    }
}