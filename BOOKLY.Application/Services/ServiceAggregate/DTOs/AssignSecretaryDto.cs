using System.ComponentModel.DataAnnotations;

namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    public sealed record AssignSecretaryDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El secretary ID debe ser mayor a 0")]
        public int SecretaryId { get; init; }
    }
}
