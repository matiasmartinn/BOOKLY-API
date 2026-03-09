using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.UserAggregate.DTOs
{
    public sealed class UpdateUserDto
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "El apellido es requerido")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
        [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
        public string Email { get; set; } = null!;
    }
}
