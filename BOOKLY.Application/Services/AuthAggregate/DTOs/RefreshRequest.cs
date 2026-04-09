using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.AuthAggregate.DTOs
{
    public sealed class RefreshRequest
    {
        [Required(ErrorMessage = "El refresh token es requerido")]
        public string RefreshToken { get; set; } = null!;
    }
}
