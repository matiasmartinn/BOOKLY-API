using BOOKLY.Domain.Queries;

namespace BOOKLY.Domain.Interfaces
{
    public interface IAdminRepository
    {
        Task<AdminDashboardSummaryReadModel> GetDashboardSummary(
            DateOnly today,
            DateTime recentActiveOwnersSince,
            CancellationToken ct = default);

        Task<IReadOnlyCollection<AdminPlanDistributionReadModel>> GetPlanDistribution(
            DateOnly today,
            CancellationToken ct = default);

        Task<IReadOnlyCollection<AdminPeriodCountReadModel>> GetOwnerCreationCountsByMonth(
            DateOnly fromMonth,
            DateOnly toMonth,
            CancellationToken ct = default);

        Task<IReadOnlyCollection<AdminPeriodCountReadModel>> GetServiceCreationCountsByMonth(
            DateOnly fromMonth,
            DateOnly toMonth,
            CancellationToken ct = default);

        Task<IReadOnlyCollection<AdminPeriodCountReadModel>> GetOwnerLoginCountsByMonth(
            DateOnly fromMonth,
            DateOnly toMonth,
            CancellationToken ct = default);

        Task<IReadOnlyCollection<AdminServiceTypeUsageReadModel>> GetServiceTypeUsage(
            CancellationToken ct = default);

        Task<(IReadOnlyCollection<AdminOwnerListItemReadModel> Items, int TotalCount)> GetOwners(
            AdminOwnerListQuery query,
            DateOnly today,
            CancellationToken ct = default);

        Task<(IReadOnlyCollection<AdminServiceListItemReadModel> Items, int TotalCount)> GetServices(
            AdminServiceListQuery query,
            DateOnly today,
            CancellationToken ct = default);
    }
}
