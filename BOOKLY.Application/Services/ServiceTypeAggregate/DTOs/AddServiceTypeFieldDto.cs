using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.ServiceTypeAggregate.DTOs
{
    public sealed record AddServiceTypeFieldDto
    {
        [Required(ErrorMessage = "La key es requerida")]
        [StringLength(60, MinimumLength = 1,
            ErrorMessage = "La key debe tener entre 1 y 60 caracteres")]
        [RegularExpression(@"^[a-z0-9_]+$",
            ErrorMessage = "La key solo puede contener letras minúsculas, números y guiones bajos")]
        public string Key { get; init; } = null!;

        [Required(ErrorMessage = "El label es requerido")]
        [StringLength(80, MinimumLength = 1,
            ErrorMessage = "El label debe tener entre 1 y 80 caracteres")]
        public string Label { get; init; } = null!;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; init; }

        [Required]
        public int FieldType { get; init; }

        [Required]
        public bool IsRequired { get; init; }

        [Range(0, int.MaxValue, ErrorMessage = "El orden no puede ser negativo")]
        public int SortOrder { get; init; }
    }
}