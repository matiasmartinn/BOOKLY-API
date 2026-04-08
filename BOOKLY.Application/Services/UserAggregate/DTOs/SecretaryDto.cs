namespace BOOKLY.Application.Services.UserAggregate.DTOs
{
    public sealed record SecretaryDto
    {
        public int Id { get; init; }
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; init; } = null!;
        public bool IsActive { get; init; }
        public bool EmailConfirmed { get; init; }
        public string Status { get; init; } = null!;
        public IReadOnlyCollection<int> ServiceIds { get; init; } = [];
    }
}
