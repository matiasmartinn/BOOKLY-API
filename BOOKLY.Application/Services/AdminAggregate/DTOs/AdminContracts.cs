namespace BOOKLY.Application.Services.AdminAggregate.DTOs
{
    public sealed record AdminPlanDto
    {
        public int Code { get; init; }
        public string Key { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
    }

    public sealed record AdminPlanDistributionItemDto
    {
        public AdminPlanDto Plan { get; init; } = null!;
        public int TotalOwners { get; init; }
    }

    public sealed record AdminStatusCountDto
    {
        public string Status { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public int Total { get; init; }
    }

    public sealed record AdminTimeSeriesPointDto
    {
        public DateOnly PeriodStart { get; init; }
        public string PeriodLabel { get; init; } = string.Empty;
        public int Total { get; init; }
    }

    public sealed record AdminServiceTypeUsageItemDto
    {
        public int ServiceTypeId { get; init; }
        public string ServiceTypeName { get; init; } = string.Empty;
        public int Total { get; init; }
    }

    public sealed record AdminDashboardSummaryDto
    {
        public int TotalUsers { get; init; }
        public int TotalOwners { get; init; }
        public int TotalServices { get; init; }
        public int TotalActiveSubscriptions { get; init; }
        public int TotalPaidSubscriptions { get; init; }
        public int ActiveOwners { get; init; }
        public int DisabledOwners { get; init; }
        public int PendingConfirmationOwners { get; init; }
        public int PendingInvitationOwners { get; init; }
        public int ActiveServices { get; init; }
        public int DisabledServices { get; init; }
        public int RecentActiveOwners { get; init; }
    }

    public sealed record AdminDashboardDto
    {
        public DateTime GeneratedAt { get; init; }
        public int RecentMonths { get; init; }
        public int RecentActiveOwnerWindowDays { get; init; }
        public AdminDashboardSummaryDto Summary { get; init; } = null!;
        public IReadOnlyCollection<AdminPlanDistributionItemDto> PlanDistribution { get; init; } = [];
        public IReadOnlyCollection<AdminTimeSeriesPointDto> OwnerRegistrations { get; init; } = [];
        public IReadOnlyCollection<AdminTimeSeriesPointDto> ServiceRegistrations { get; init; } = [];
        public IReadOnlyCollection<AdminServiceTypeUsageItemDto> ServiceTypeUsage { get; init; } = [];
        public IReadOnlyCollection<AdminTimeSeriesPointDto> OwnerRecentActivity { get; init; } = [];
        public IReadOnlyCollection<AdminStatusCountDto> OwnerStatusDistribution { get; init; } = [];
        public IReadOnlyCollection<AdminStatusCountDto> ServiceStatusDistribution { get; init; } = [];
    }

    public sealed record AdminPagedResultDto<T>
    {
        public IReadOnlyCollection<T> Items { get; init; } = [];
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
    }

    public sealed record AdminOwnerListItemDto
    {
        public int Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string AccountStatus { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public bool EmailConfirmed { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public int ServiceCount { get; init; }
        public AdminPlanDto CurrentPlan { get; init; } = null!;
        public bool HasPersistedSubscription { get; init; }
        public string SubscriptionStatus { get; init; } = string.Empty;
        public DateOnly? SubscriptionStartDate { get; init; }
        public DateOnly? SubscriptionEndDate { get; init; }
    }

    public sealed record AdminServiceListItemDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public int OwnerId { get; init; }
        public string OwnerName { get; init; } = string.Empty;
        public string OwnerEmail { get; init; } = string.Empty;
        public string OwnerAccountStatus { get; init; } = string.Empty;
        public AdminPlanDto OwnerPlan { get; init; } = null!;
        public int ServiceTypeId { get; init; }
        public string ServiceTypeName { get; init; } = string.Empty;
        public decimal? Price { get; init; }
    }
}
