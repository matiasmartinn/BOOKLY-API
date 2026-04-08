using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;

namespace BOOKLY.Domain.Queries
{
    public sealed record AdminDashboardSummaryReadModel(
        int TotalUsers,
        int TotalOwners,
        int TotalServices,
        int ActiveOwners,
        int DisabledOwners,
        int PendingConfirmationOwners,
        int PendingInvitationOwners,
        int ActiveServices,
        int DisabledServices,
        int ActiveSubscriptions,
        int PaidSubscriptions,
        int RecentActiveOwners);

    public sealed record AdminPlanDistributionReadModel(
        PlanName PlanName,
        int TotalOwners);

    public sealed record AdminPeriodCountReadModel(
        DateOnly PeriodStart,
        int Total);

    public sealed record AdminServiceTypeUsageReadModel(
        int ServiceTypeId,
        string ServiceTypeName,
        int Total);

    public sealed record AdminOwnerListQuery(
        string? Search,
        string? Status,
        PlanName? Plan,
        DateOnly? CreatedFrom,
        DateOnly? CreatedTo,
        int Page,
        int PageSize);

    public sealed record AdminServiceListQuery(
        string? Search,
        string? Status,
        int? OwnerId,
        string? OwnerSearch,
        PlanName? Plan,
        int Page,
        int PageSize);

    public sealed record AdminOwnerListItemReadModel(
        int Id,
        string FirstName,
        string LastName,
        string Email,
        UserStatus Status,
        bool IsActive,
        bool EmailConfirmed,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        int ServiceCount,
        PlanName EffectivePlanName,
        bool HasPersistedSubscription,
        SubscriptionStatus? SubscriptionStatus,
        DateOnly? SubscriptionStartDate,
        DateOnly? SubscriptionEndDate);

    public sealed record AdminServiceListItemReadModel(
        int Id,
        string Name,
        string Slug,
        bool IsActive,
        DateTime CreatedAt,
        int OwnerId,
        string OwnerFirstName,
        string OwnerLastName,
        string OwnerEmail,
        UserStatus OwnerStatus,
        bool OwnerIsActive,
        bool OwnerEmailConfirmed,
        PlanName OwnerEffectivePlanName,
        int ServiceTypeId,
        string ServiceTypeName,
        decimal? Price);
}
