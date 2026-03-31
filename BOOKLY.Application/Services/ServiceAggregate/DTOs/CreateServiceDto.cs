using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    public sealed record CreateServiceDto
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        public string Name { get; init; } = null!;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El owner ID debe ser mayor a 0")]
        public int OwnerId { get; init; }

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; init; }

        [StringLength(150, ErrorMessage = "El nombre del lugar no puede exceder 150 caracteres")]
        public string? PlaceName { get; init; }

        [StringLength(250, ErrorMessage = "La dirección no puede exceder 250 caracteres")]
        public string? Address { get; init; }

        [Url(ErrorMessage = "La URL de Google Maps no es válida")]
        [StringLength(500, ErrorMessage = "La URL de Google Maps no puede exceder 500 caracteres")]
        public string? GoogleMapsUrl { get; init; }

        public string? Slug { get; init; }

        [Required]
        [Range(1, int.MaxValue)]
        public int ServiceTypeId { get; init; }

        [Required]
        [Range(5, 480, ErrorMessage = "La duración debe estar entre 5 y 480 minutos")]
        public int DurationMinutes { get; init; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La capacidad debe ser mayor a 0")]
        public int Capacity { get; init; }

        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
        public decimal? Price { get; init; }

        [Required]
        public List<CreateServiceScheduleDto> Schedules { get; init; } = null!;
    }
}
