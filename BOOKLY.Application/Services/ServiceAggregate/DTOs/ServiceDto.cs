using BOOKLY.Application.Services.ServiceTypeAggregate.DTOs;

namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    public record ServiceDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = null!;
        public int OwnerId { get; init; }
        public string Slug { get; init; } = null!;
        public string? Description { get; init; }
        public string? PhoneNumber { get; init; }
        public string? PlaceName { get; init; }
        public string? Address { get; init; }
        public int ServiceTypeId { get; init; }
        public DateTime CreatedAt { get; init; }
        public int DurationMinutes { get; init; }
        public int Capacity { get; init; }
        public string Mode { get; init; } = null!;
        public bool IsActive { get; init; }
        public decimal? Price { get; init; }
        public bool IsPublicBookingEnabled { get; init; }
        public string PublicBookingCode { get; init; } = null!;
        public DateTime? PublicBookingCodeUpdatedAt { get; init; }
        public bool AllowsExtraFields { get; init; }
        public List<ServiceTypeFieldDefinitionDto> FieldDefinitions { get; init; } = new();
        public List<int?> SecretaryIds { get; init; } = new();
        public List<ServiceSecretaryPermissionsDto> SecretaryPermissions { get; init; } = new();
        public List<ServiceScheduleDto?> Schedules { get; init; } = new();
    }
}
