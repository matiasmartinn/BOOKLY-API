using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.UserAggregate.DTOs
{
    public sealed record RequestPasswordResetDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = null!;
    }
}
