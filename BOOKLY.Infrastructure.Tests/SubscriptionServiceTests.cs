using AutoMapper;
using BOOKLY.Application.Services.SubscriptionAggregate;
using BOOKLY.Application.Services.SubscriptionAggregate.Dto;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;

namespace BOOKLY.Infrastructure.Tests;

public sealed class SubscriptionServiceTests
{
    private static readonly DateTime ArgentinaNow = new(2026, 3, 31, 10, 0, 0);

    [Fact]
    public async Task Renew_ShouldApplyAutomaticMonthlyPeriodFromArgentinaToday()
    {
        var subscription = Subscription.CreatePaid(
            1,
            SubscriptionPlan.Pro(),
            SubscriptionPeriod.Create(new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1)),
            ArgentinaNow.AddMonths(-1));

        subscription.Cancel(ArgentinaNow.AddDays(-1));

        var repository = new FakeSubscriptionRepository(subscription);
        var service = CreateService(repository, ArgentinaNow);

        var result = await service.Renew(new RenewSubscriptionDto { OwnerId = 1 });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(new DateOnly(2026, 3, 31), result.Data!.StartDate);
        Assert.Equal(new DateOnly(2026, 4, 30), result.Data.EndDate);
        Assert.Equal(SubscriptionStatus.Active, repository.Subscription!.Status);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task ChangePlan_FromFreeToPaid_ShouldCreateAutomaticMonthlyPeriod()
    {
        var subscription = Subscription.CreateFree(1, ArgentinaNow.AddMonths(-1));
        var repository = new FakeSubscriptionRepository(subscription);
        var service = CreateService(repository, ArgentinaNow);

        var result = await service.ChangePlan(new ChangePlanDto
        {
            OwnerId = 1,
            TargetPlan = "pro"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(PlanName.Pro, repository.Subscription!.Plan.Name);
        Assert.Equal(new DateOnly(2026, 3, 31), repository.Subscription.Period.StartDate);
        Assert.Equal(new DateOnly(2026, 4, 30), repository.Subscription.Period.EndDate);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task ChangePlan_BetweenPaidPlans_ShouldRenewWithTheSameAutomaticMonthlyRule()
    {
        var subscription = Subscription.CreatePaid(
            1,
            SubscriptionPlan.Pro(),
            SubscriptionPeriod.Create(new DateOnly(2026, 2, 10), new DateOnly(2026, 3, 10)),
            ArgentinaNow.AddMonths(-1));

        subscription.Cancel(ArgentinaNow.AddDays(-2));

        var repository = new FakeSubscriptionRepository(subscription);
        var service = CreateService(repository, ArgentinaNow);

        var result = await service.ChangePlan(new ChangePlanDto
        {
            OwnerId = 1,
            TargetPlan = "max"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(PlanName.Max, repository.Subscription!.Plan.Name);
        Assert.Equal(SubscriptionStatus.Active, repository.Subscription.Status);
        Assert.Equal(new DateOnly(2026, 3, 31), repository.Subscription.Period.StartDate);
        Assert.Equal(new DateOnly(2026, 4, 30), repository.Subscription.Period.EndDate);
        Assert.True(repository.WasUpdated);
    }

    private static SubscriptionService CreateService(FakeSubscriptionRepository repository, DateTime now)
    {
        return new SubscriptionService(
            repository,
            new FakeServiceRepository(),
            new StubDateTimeProvider(now),
            new FakeUnitOfWork(),
            CreateMapper());
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddMaps(typeof(SubscriptionService).Assembly));
        return configuration.CreateMapper();
    }

    private sealed class FakeSubscriptionRepository : ISubscriptionRepository
    {
        public FakeSubscriptionRepository(Subscription? subscription)
        {
            Subscription = subscription;
        }

        public Subscription? Subscription { get; private set; }
        public bool WasUpdated { get; private set; }

        public Task<Subscription?> GetByOwnerId(int ownerId, CancellationToken ct = default)
        {
            return Task.FromResult(Subscription);
        }

        public Task<Subscription?> GetByOwnerIdForUpdate(int ownerId, CancellationToken ct = default)
        {
            return Task.FromResult(Subscription);
        }

        public Task AddOne(Subscription subscription, CancellationToken ct = default)
        {
            Subscription = subscription;
            return Task.CompletedTask;
        }

        public void Update(Subscription subscription)
        {
            Subscription = subscription;
            WasUpdated = true;
        }
    }

    private sealed class FakeServiceRepository : IServiceRepository
    {
        public Task<Service?> GetOne(int id, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<Service?> GetOneWithSchedules(int id, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<Service?> GetOneWithUnavailability(int id, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<Service?> GetOneWithSecretaries(int id, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<Service?> GetOneWithSchedulesAndUnavailability(int id, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<List<ServiceSchedule>> GetSchedulesByService(int serviceId, CancellationToken ct = default) => Task.FromResult(new List<ServiceSchedule>());
        public Task<List<ServiceUnavailability>> GetUnavailabilityByService(int serviceId, CancellationToken ct = default) => Task.FromResult(new List<ServiceUnavailability>());
        public Task<List<Service>> GetServicesByOwner(int ownerId, CancellationToken ct = default) => Task.FromResult(new List<Service>());
        public Task<List<Service>> GetServicesByOwnerWithSecretaries(int ownerId, CancellationToken ct = default) => Task.FromResult(new List<Service>());
        public Task<List<int>> GetServiceIdsBySecretary(int secretaryId, CancellationToken ct = default) => Task.FromResult(new List<int>());
        public Task<bool> ExistsSlug(string slug, int? excludedServiceId = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> ExistsBlock(int id, DateTime startDateTime, DateTime endDateTime, CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> CountByOwnerId(int ownerId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> CountAssignedSecretariesByOwnerId(int ownerId, CancellationToken ct = default) => Task.FromResult(0);
        public Task AddOne(Service service, CancellationToken ct = default) => Task.CompletedTask;
        public void Update(Service service) { }
        public void Remove(Service service) { }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChanges(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider
    {
        private readonly DateTime _now;

        public StubDateTimeProvider(DateTime now)
        {
            _now = now;
        }

        public DateTime NowArgentina() => _now;
        public DateTime UtcNow() => _now.ToUniversalTime();
    }
}
