using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.UserAggregate.DTOs
{
    public sealed class ChangePasswordDto
    {
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        public string CurrentPassword { get; set; } = null!;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 128 caracteres")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe contener al menos una mayúscula y un número")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
        [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
