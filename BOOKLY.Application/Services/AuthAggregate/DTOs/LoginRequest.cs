using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.AuthAggregate.DTOs
{
    public sealed class LoginRequest
    {
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es requerida")]
        public string Password { get; set; } = null!;
    }
}
