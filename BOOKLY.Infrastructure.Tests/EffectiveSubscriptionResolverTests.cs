using BOOKLY.Application.Services.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;

namespace BOOKLY.Infrastructure.Tests;

public sealed class EffectiveSubscriptionResolverTests
{
    private static readonly DateTime ReferenceNow = new(2026, 3, 15, 10, 0, 0);
    private static readonly DateOnly Today = DateOnly.FromDateTime(ReferenceNow);

    [Fact]
    public async Task Resolve_ShouldReturnImplicitFree_WhenSubscriptionDoesNotExist()
    {
        var repository = new FakeSubscriptionRepository(null);
        var resolver = CreateResolver(repository);

        var subscription = await resolver.Resolve(ownerId: 10);

        Assert.Equal(PlanName.Free, subscription.Plan.Name);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.True(subscription.Period.IsOpenEnded);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, repository.UpdateCalls);
    }

    [Fact]
    public async Task Resolve_ShouldReturnPaidSubscription_WhenPaidIsActive()
    {
        var persisted = CreatePaid(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));
        var repository = new FakeSubscriptionRepository(persisted);
        var resolver = CreateResolver(repository);

        var subscription = await resolver.Resolve(ownerId: 10);

        Assert.Same(persisted, subscription);
        Assert.Equal(PlanName.Pro, subscription.Plan.Name);
    }

    [Fact]
    public async Task Resolve_ShouldReturnImplicitFree_WhenPaidIsExpired()
    {
        var persisted = CreatePaid(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));
        var repository = new FakeSubscriptionRepository(persisted);
        var resolver = CreateResolver(repository);

        var subscription = await resolver.Resolve(ownerId: 10);

        Assert.NotSame(persisted, subscription);
        Assert.Equal(PlanName.Free, subscription.Plan.Name);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, repository.UpdateCalls);
    }

    [Fact]
    public async Task Resolve_ShouldReturnPaidSubscription_WhenCancelledHasNotExpired()
    {
        var persisted = CreatePaid(new DateOnly(2026, 3, 1), Today);
        persisted.Cancel(ReferenceNow.AddDays(-1));
        var repository = new FakeSubscriptionRepository(persisted);
        var resolver = CreateResolver(repository);

        var subscription = await resolver.Resolve(ownerId: 10);

        Assert.Same(persisted, subscription);
        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        Assert.Equal(PlanName.Pro, subscription.Plan.Name);
    }

    [Fact]
    public async Task Resolve_ShouldReturnImplicitFree_WhenCancelledIsExpired()
    {
        var persisted = CreatePaid(new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 14));
        persisted.Cancel(ReferenceNow.AddDays(-2));
        var repository = new FakeSubscriptionRepository(persisted);
        var resolver = CreateResolver(repository);

        var subscription = await resolver.Resolve(ownerId: 10);

        Assert.NotSame(persisted, subscription);
        Assert.Equal(PlanName.Free, subscription.Plan.Name);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, repository.UpdateCalls);
    }

    private static EffectiveSubscriptionResolver CreateResolver(FakeSubscriptionRepository repository)
        => new(repository, new StubDateTimeProvider());

    private static Subscription CreatePaid(DateOnly start, DateOnly end)
        => Subscription.CreatePaid(
            10,
            SubscriptionPlan.Pro(),
            SubscriptionPeriod.Create(start, end),
            ReferenceNow.AddMonths(-1));

    private sealed class FakeSubscriptionRepository(Subscription? subscription) : ISubscriptionRepository
    {
        public int AddCalls { get; private set; }
        public int UpdateCalls { get; private set; }

        public Task<Subscription?> GetByOwnerId(int ownerId, CancellationToken ct = default)
            => Task.FromResult(subscription);

        public Task<Subscription?> GetByOwnerIdTracked(int ownerId, CancellationToken ct = default)
            => Task.FromResult(subscription);

        public Task AddOne(Subscription subscription, CancellationToken ct = default)
        {
            AddCalls++;
            return Task.CompletedTask;
        }

        public void Update(Subscription subscription)
        {
            UpdateCalls++;
        }
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider
    {
        public DateTime NowArgentina() => ReferenceNow;
        public DateTime UtcNow() => ReferenceNow.ToUniversalTime();
    }
}
