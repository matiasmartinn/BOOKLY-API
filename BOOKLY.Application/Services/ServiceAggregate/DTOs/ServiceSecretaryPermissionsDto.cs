using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;

namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    public sealed record ServiceSecretaryPermissionsDto
    {
        public int SecretaryId { get; init; }
        public List<SecretaryPermission> Permissions { get; init; } = [];
    }
}
