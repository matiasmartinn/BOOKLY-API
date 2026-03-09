using BOOKLY.Application.Services.UserAggregate.DTOs;
using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.UserAggregate
{
    public partial class UserService
    {
        public record CreateSecretaryDto
        {
            [Required(ErrorMessage = "El nombre es requerido")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
            public string FirstName { get; init; } = null!;

            [Required(ErrorMessage = "El apellido es requerido")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres")]
            public string LastName { get; init; } = null!;

            [Required(ErrorMessage = "El email es requerido")]
            [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
            [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
            public string Email { get; init; } = null!;

            [Range(1, int.MaxValue, ErrorMessage = "El serviceId debe ser mayor a 0")]
            public int ServiceId { get; init; }
        }
    }
}
