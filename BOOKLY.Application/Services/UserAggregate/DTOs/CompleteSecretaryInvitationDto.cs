using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.UserAggregate.DTOs
{
    public record CompleteSecretaryInvitationDto
    {
        [Required]
        public string Token { get; init; } = null!;

        [Required]
        [StringLength(128, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$")]
        public string Password { get; init; } = null!;
    }
}
