using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.UserAggregate.DTOs
{
    public sealed record ConfirmEmailDto
    {
        [Required]
        public string Token { get; init; } = null!;
    }
}
