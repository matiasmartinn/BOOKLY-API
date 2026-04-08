using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Queries;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Repositories
{
    public sealed class AdminRepository : IAdminRepository
    {
        private readonly BooklyDbContext _dbContext;

        public AdminRepository(BooklyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AdminDashboardSummaryReadModel> GetDashboardSummary(
            DateOnly today,
            DateTime recentActiveOwnersSince,
            CancellationToken ct = default)
        {
            var totalUsers = await _dbContext.Users
                .AsNoTracking()
                .CountAsync(ct);

            var owners = _dbContext.Users
                .AsNoTracking()
                .Where(user => user.Role == UserRole.Owner);

            var totalOwners = await owners.CountAsync(ct);
            var activeOwners = await owners.CountAsync(user => user.Status == UserStatus.Active, ct);
            var disabledOwners = await owners.CountAsync(user => user.Status == UserStatus.Inactive, ct);
            var pendingConfirmationOwners = await owners.CountAsync(user => user.Status == UserStatus.PendingEmailConfirmation, ct);
            var pendingInvitationOwners = await owners.CountAsync(user => user.Status == UserStatus.PendingInvitationAcceptance, ct);
            var recentActiveOwners = await owners.CountAsync(
                user => user.LastLoginAt.HasValue && user.LastLoginAt.Value >= recentActiveOwnersSince,
                ct);

            var totalServices = await _dbContext.Services
                .AsNoTracking()
                .CountAsync(ct);

            var activeServices = await _dbContext.Services
                .AsNoTracking()
                .CountAsync(service => service.IsActive, ct);

            var activeSubscriptions = await BuildCurrentSubscriptionQuery(today)
                .CountAsync(ct);

            var paidSubscriptions = await BuildCurrentSubscriptionQuery(today)
                .CountAsync(subscription => subscription.PlanName != PlanName.Free, ct);

            return new AdminDashboardSummaryReadModel(
                TotalUsers: totalUsers,
                TotalOwners: totalOwners,
                TotalServices: totalServices,
                ActiveOwners: activeOwners,
                DisabledOwners: disabledOwners,
                PendingConfirmationOwners: pendingConfirmationOwners,
                PendingInvitationOwners: pendingInvitationOwners,
                ActiveServices: activeServices,
                DisabledServices: totalServices - activeServices,
                ActiveSubscriptions: activeSubscriptions,
                PaidSubscriptions: paidSubscriptions,
                RecentActiveOwners: recentActiveOwners);
        }

        public async Task<IReadOnlyCollection<AdminPlanDistributionReadModel>> GetPlanDistribution(
            DateOnly today,
            CancellationToken ct = default)
        {
            var ownerIds = await _dbContext.Users
                .AsNoTracking()
                .Where(owner => owner.Role == UserRole.Owner)
                .Select(owner => owner.Id)
                .ToListAsync(ct);

            var subscriptionsByOwnerId = await GetSubscriptionsByOwnerIds(ownerIds, ct);

            return ownerIds
                .GroupBy(ownerId => GetEffectivePlan(subscriptionsByOwnerId.GetValueOrDefault(ownerId), today))
                .Select(group => new AdminPlanDistributionReadModel(group.Key, group.Count()))
                .ToList();
        }

        public async Task<IReadOnlyCollection<AdminPeriodCountReadModel>> GetOwnerCreationCountsByMonth(
            DateOnly fromMonth,
            DateOnly toMonth,
            CancellationToken ct = default)
        {
            var fromInclusive = ToMonthStart(fromMonth);
            var toExclusive = ToMonthStart(toMonth).AddMonths(1);

            var rows = await _dbContext.Users
                .AsNoTracking()
                .Where(user =>
                    user.Role == UserRole.Owner &&
                    user.CreatedAt >= fromInclusive &&
                    user.CreatedAt < toExclusive)
                .GroupBy(user => new { user.CreatedAt.Year, user.CreatedAt.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Total = group.Count()
                })
                .OrderBy(group => group.Year)
                .ThenBy(group => group.Month)
                .ToListAsync(ct);

            return rows
                .Select(row => new AdminPeriodCountReadModel(
                    new DateOnly(row.Year, row.Month, 1),
                    row.Total))
                .ToList();
        }

        public async Task<IReadOnlyCollection<AdminPeriodCountReadModel>> GetServiceCreationCountsByMonth(
            DateOnly fromMonth,
            DateOnly toMonth,
            CancellationToken ct = default)
        {
            var fromInclusive = ToMonthStart(fromMonth);
            var toExclusive = ToMonthStart(toMonth).AddMonths(1);

            var rows = await _dbContext.Services
                .AsNoTracking()
                .Where(service =>
                    service.CreatedAt >= fromInclusive &&
                    service.CreatedAt < toExclusive)
                .GroupBy(service => new { service.CreatedAt.Year, service.CreatedAt.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Total = group.Count()
                })
                .OrderBy(group => group.Year)
                .ThenBy(group => group.Month)
                .ToListAsync(ct);

            return rows
                .Select(row => new AdminPeriodCountReadModel(
                    new DateOnly(row.Year, row.Month, 1),
                    row.Total))
                .ToList();
        }

        public async Task<IReadOnlyCollection<AdminPeriodCountReadModel>> GetOwnerLoginCountsByMonth(
            DateOnly fromMonth,
            DateOnly toMonth,
            CancellationToken ct = default)
        {
            var fromInclusive = ToMonthStart(fromMonth);
            var toExclusive = ToMonthStart(toMonth).AddMonths(1);

            var rows = await _dbContext.Users
                .AsNoTracking()
                .Where(user =>
                    user.Role == UserRole.Owner &&
                    user.LastLoginAt.HasValue &&
                    user.LastLoginAt.Value >= fromInclusive &&
                    user.LastLoginAt.Value < toExclusive)
                .GroupBy(user => new { user.LastLoginAt!.Value.Year, user.LastLoginAt!.Value.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Total = group.Count()
                })
                .OrderBy(group => group.Year)
                .ThenBy(group => group.Month)
                .ToListAsync(ct);

            return rows
                .Select(row => new AdminPeriodCountReadModel(
                    new DateOnly(row.Year, row.Month, 1),
                    row.Total))
                .ToList();
        }

        public async Task<IReadOnlyCollection<AdminServiceTypeUsageReadModel>> GetServiceTypeUsage(
            CancellationToken ct = default)
        {
            return await (
                from service in _dbContext.Services.AsNoTracking()
                join serviceType in _dbContext.ServiceTypes.AsNoTracking()
                    on service.ServiceTypeId equals serviceType.Id
                group service by new
                {
                    service.ServiceTypeId,
                    serviceType.Name
                }
                into grouped
                orderby grouped.Count() descending, grouped.Key.Name
                select new AdminServiceTypeUsageReadModel(
                    grouped.Key.ServiceTypeId,
                    grouped.Key.Name,
                    grouped.Count()))
                .ToListAsync(ct);
        }

        public async Task<(IReadOnlyCollection<AdminOwnerListItemReadModel> Items, int TotalCount)> GetOwners(
            AdminOwnerListQuery query,
            DateOnly today,
            CancellationToken ct = default)
        {
            var currentSubscriptions = BuildCurrentSubscriptionQuery(today);

            var ownerQuery = BuildOwnerListQuery();
            ownerQuery = ApplyOwnerFilters(ownerQuery, query, currentSubscriptions);

            var totalCount = await ownerQuery.CountAsync(ct);

            var pageOwners = await ownerQuery
                .OrderByDescending(owner => owner.CreatedAt)
                .ThenByDescending(owner => owner.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            var ownerIds = pageOwners.Select(owner => owner.Id).ToList();
            var serviceCountsByOwnerId = await _dbContext.Services
                .AsNoTracking()
                .Where(service => ownerIds.Contains(service.OwnerId))
                .GroupBy(service => service.OwnerId)
                .Select(group => new
                {
                    OwnerId = group.Key,
                    Total = group.Count()
                })
                .ToDictionaryAsync(group => group.OwnerId, group => group.Total, ct);

            var subscriptionsByOwnerId = await GetSubscriptionsByOwnerIds(ownerIds, ct);

            var items = pageOwners
                .Select(owner =>
                {
                    var subscription = subscriptionsByOwnerId.GetValueOrDefault(owner.Id);

                    return new AdminOwnerListItemReadModel(
                        owner.Id,
                        owner.FirstName,
                        owner.LastName,
                        owner.Email,
                        owner.Status,
                        owner.IsActive,
                        owner.EmailConfirmed,
                        owner.CreatedAt,
                        owner.LastLoginAt,
                        serviceCountsByOwnerId.GetValueOrDefault(owner.Id, 0),
                        GetEffectivePlan(subscription, today),
                        subscription != null,
                        subscription?.Status,
                        subscription?.StartDate,
                        subscription?.EndDate);
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<(IReadOnlyCollection<AdminServiceListItemReadModel> Items, int TotalCount)> GetServices(
            AdminServiceListQuery query,
            DateOnly today,
            CancellationToken ct = default)
        {
            var currentSubscriptions = BuildCurrentSubscriptionQuery(today);

            var serviceQuery = BuildServiceListQuery();
            serviceQuery = ApplyServiceFilters(serviceQuery, query, currentSubscriptions);

            var totalCount = await serviceQuery.CountAsync(ct);

            var pageServices = await serviceQuery
                .OrderByDescending(service => service.CreatedAt)
                .ThenByDescending(service => service.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            var ownerIds = pageServices.Select(service => service.OwnerId).Distinct().ToList();
            var subscriptionsByOwnerId = await GetSubscriptionsByOwnerIds(ownerIds, ct);

            var items = pageServices
                .Select(service =>
                {
                    var subscription = subscriptionsByOwnerId.GetValueOrDefault(service.OwnerId);

                    return new AdminServiceListItemReadModel(
                        service.Id,
                        service.Name,
                        service.Slug,
                        service.IsActive,
                        service.CreatedAt,
                        service.OwnerId,
                        service.OwnerFirstName,
                        service.OwnerLastName,
                        service.OwnerEmail,
                        service.OwnerStatus,
                        service.OwnerIsActive,
                        service.OwnerEmailConfirmed,
                        GetEffectivePlan(subscription, today),
                        service.ServiceTypeId,
                        service.ServiceTypeName,
                        service.Price);
                })
                .ToList();

            return (items, totalCount);
        }

        private IQueryable<AdminOwnerListRow> BuildOwnerListQuery()
        {
            return _dbContext.Users
                .AsNoTracking()
                .Where(owner => owner.Role == UserRole.Owner)
                .Select(owner => new AdminOwnerListRow
                {
                    Id = owner.Id,
                    FirstName = owner.PersonName.FirstName,
                    LastName = owner.PersonName.LastName,
                    Email = owner.Email.Value,
                    Status = owner.Status,
                    IsActive = owner.IsActive,
                    EmailConfirmed = owner.EmailConfirmed,
                    CreatedAt = owner.CreatedAt,
                    LastLoginAt = owner.LastLoginAt
                });
        }

        private static IQueryable<AdminOwnerListRow> ApplyOwnerFilters(
            IQueryable<AdminOwnerListRow> ownerQuery,
            AdminOwnerListQuery query,
            IQueryable<SubscriptionSnapshot> currentSubscriptions)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                ownerQuery = ownerQuery.Where(owner =>
                    owner.FirstName.Contains(search) ||
                    owner.LastName.Contains(search) ||
                    owner.Email.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                ownerQuery = query.Status switch
                {
                    "active" => ownerQuery.Where(owner => owner.Status == UserStatus.Active),
                    "disabled" => ownerQuery.Where(owner => owner.Status == UserStatus.Inactive),
                    "pending_email_confirmation" => ownerQuery.Where(owner => owner.Status == UserStatus.PendingEmailConfirmation),
                    "pending_invitation_acceptance" => ownerQuery.Where(owner => owner.Status == UserStatus.PendingInvitationAcceptance),
                    _ => ownerQuery
                };
            }

            if (query.Plan.HasValue)
            {
                ownerQuery = query.Plan.Value == PlanName.Free
                    ? ownerQuery.Where(owner =>
                        !currentSubscriptions.Any(subscription =>
                            subscription.OwnerId == owner.Id &&
                            subscription.PlanName != PlanName.Free))
                    : ownerQuery.Where(owner =>
                        currentSubscriptions.Any(subscription =>
                            subscription.OwnerId == owner.Id &&
                            subscription.PlanName == query.Plan.Value));
            }

            if (query.CreatedFrom.HasValue)
            {
                var createdFrom = query.CreatedFrom.Value.ToDateTime(TimeOnly.MinValue);
                ownerQuery = ownerQuery.Where(owner => owner.CreatedAt >= createdFrom);
            }

            if (query.CreatedTo.HasValue)
            {
                var createdToExclusive = query.CreatedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                ownerQuery = ownerQuery.Where(owner => owner.CreatedAt < createdToExclusive);
            }

            return ownerQuery;
        }

        private IQueryable<AdminServiceListRow> BuildServiceListQuery()
        {
            return
                from service in _dbContext.Services.AsNoTracking()
                join owner in _dbContext.Users.AsNoTracking()
                    on service.OwnerId equals owner.Id
                join serviceType in _dbContext.ServiceTypes.AsNoTracking()
                    on service.ServiceTypeId equals serviceType.Id into serviceTypes
                from serviceType in serviceTypes.DefaultIfEmpty()
                select new AdminServiceListRow
                {
                    Id = service.Id,
                    Name = service.Name,
                    Slug = service.Slug.Value,
                    IsActive = service.IsActive,
                    CreatedAt = service.CreatedAt,
                    ServiceTypeId = service.ServiceTypeId,
                    ServiceTypeName = serviceType != null ? serviceType.Name : string.Empty,
                    Price = service.Price,
                    OwnerId = owner.Id,
                    OwnerFirstName = owner.PersonName.FirstName,
                    OwnerLastName = owner.PersonName.LastName,
                    OwnerEmail = owner.Email.Value,
                    OwnerStatus = owner.Status,
                    OwnerIsActive = owner.IsActive,
                    OwnerEmailConfirmed = owner.EmailConfirmed
                };
        }

        private static IQueryable<AdminServiceListRow> ApplyServiceFilters(
            IQueryable<AdminServiceListRow> serviceQuery,
            AdminServiceListQuery query,
            IQueryable<SubscriptionSnapshot> currentSubscriptions)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                serviceQuery = serviceQuery.Where(service =>
                    service.Name.Contains(search) ||
                    service.Slug.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                serviceQuery = query.Status == "active"
                    ? serviceQuery.Where(service => service.IsActive)
                    : serviceQuery.Where(service => !service.IsActive);
            }

            if (query.OwnerId.HasValue)
            {
                serviceQuery = serviceQuery.Where(service => service.OwnerId == query.OwnerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.OwnerSearch))
            {
                var ownerSearch = query.OwnerSearch.Trim();
                serviceQuery = serviceQuery.Where(service =>
                    service.OwnerFirstName.Contains(ownerSearch) ||
                    service.OwnerLastName.Contains(ownerSearch) ||
                    service.OwnerEmail.Contains(ownerSearch));
            }

            if (query.Plan.HasValue)
            {
                serviceQuery = query.Plan.Value == PlanName.Free
                    ? serviceQuery.Where(service =>
                        !currentSubscriptions.Any(subscription =>
                            subscription.OwnerId == service.OwnerId &&
                            subscription.PlanName != PlanName.Free))
                    : serviceQuery.Where(service =>
                        currentSubscriptions.Any(subscription =>
                            subscription.OwnerId == service.OwnerId &&
                            subscription.PlanName == query.Plan.Value));
            }

            return serviceQuery;
        }

        private async Task<Dictionary<int, SubscriptionSnapshot>> GetSubscriptionsByOwnerIds(
            IReadOnlyCollection<int> ownerIds,
            CancellationToken ct = default)
        {
            if (ownerIds.Count == 0)
            {
                return [];
            }

            return await BuildSubscriptionSnapshotQuery()
                .Where(subscription => ownerIds.Contains(subscription.OwnerId))
                .ToDictionaryAsync(subscription => subscription.OwnerId, ct);
        }

        private static PlanName GetEffectivePlan(SubscriptionSnapshot? subscription, DateOnly today)
        {
            if (subscription != null &&
                (subscription.Status == SubscriptionStatus.Active ||
                 subscription.Status == SubscriptionStatus.Cancelled) &&
                (!subscription.EndDate.HasValue || subscription.EndDate.Value >= today))
            {
                return subscription.PlanName;
            }

            return PlanName.Free;
        }

        private IQueryable<SubscriptionSnapshot> BuildCurrentSubscriptionQuery(DateOnly today)
        {
            return BuildSubscriptionSnapshotQuery()
                .Where(subscription =>
                    (subscription.Status == SubscriptionStatus.Active ||
                     subscription.Status == SubscriptionStatus.Cancelled) &&
                    (!subscription.EndDate.HasValue || subscription.EndDate.Value >= today));
        }

        private IQueryable<SubscriptionSnapshot> BuildSubscriptionSnapshotQuery()
        {
            return _dbContext.Subscriptions
                .AsNoTracking()
                .Select(subscription => new SubscriptionSnapshot
                {
                    OwnerId = subscription.OwnerId,
                    Status = subscription.Status,
                    PlanName = subscription.Plan.Name,
                    StartDate = subscription.Period.StartDate,
                    EndDate = subscription.Period.EndDate
                });
        }

        private static DateTime ToMonthStart(DateOnly date)
        {
            return new DateOnly(date.Year, date.Month, 1).ToDateTime(TimeOnly.MinValue);
        }

        private sealed class SubscriptionSnapshot
        {
            public int OwnerId { get; init; }
            public SubscriptionStatus Status { get; init; }
            public PlanName PlanName { get; init; }
            public DateOnly StartDate { get; init; }
            public DateOnly? EndDate { get; init; }
        }

        private sealed class AdminOwnerListRow
        {
            public int Id { get; init; }
            public string FirstName { get; init; } = string.Empty;
            public string LastName { get; init; } = string.Empty;
            public string Email { get; init; } = string.Empty;
            public UserStatus Status { get; init; }
            public bool IsActive { get; init; }
            public bool EmailConfirmed { get; init; }
            public DateTime CreatedAt { get; init; }
            public DateTime? LastLoginAt { get; init; }
        }

        private sealed class AdminServiceListRow
        {
            public int Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Slug { get; init; } = string.Empty;
            public bool IsActive { get; init; }
            public DateTime CreatedAt { get; init; }
            public int OwnerId { get; init; }
            public string OwnerFirstName { get; init; } = string.Empty;
            public string OwnerLastName { get; init; } = string.Empty;
            public string OwnerEmail { get; init; } = string.Empty;
            public UserStatus OwnerStatus { get; init; }
            public bool OwnerIsActive { get; init; }
            public bool OwnerEmailConfirmed { get; init; }
            public int ServiceTypeId { get; init; }
            public string ServiceTypeName { get; init; } = string.Empty;
            public decimal? Price { get; init; }
        }
    }
}
