namespace BOOKLY.Application.Services.AdminAggregate.DTOs
{
    public sealed record AdminDashboardQueryDto
    {
        public int Months { get; init; } = 6;
    }

    public sealed record AdminOwnersQueryDto
    {
        public string? Search { get; init; }
        public string? Status { get; init; }
        public string? Plan { get; init; }
        public DateOnly? CreatedFrom { get; init; }
        public DateOnly? CreatedTo { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public sealed record AdminServicesQueryDto
    {
        public string? Search { get; init; }
        public string? Status { get; init; }
        public int? OwnerId { get; init; }
        public string? OwnerSearch { get; init; }
        public string? Plan { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
