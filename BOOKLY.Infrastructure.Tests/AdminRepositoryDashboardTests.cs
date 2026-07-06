using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using BOOKLY.Infrastructure.Persistence;
using BOOKLY.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Tests;

public sealed class AdminRepositoryDashboardTests
{
    private static readonly DateTime ReferenceNow = new(2026, 7, 6, 10, 0, 0);
    private static readonly DateOnly Today = DateOnly.FromDateTime(ReferenceNow);

    [Fact]
    public async Task GetDashboardSummary_ShouldCountOnlyCurrentSubscriptions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var freeOwner = CreateOwner("free@example.com");
        var paidOwner = CreateOwner("paid@example.com");
        var expiredOwner = CreateOwner("expired@example.com");

        context.Users.AddRange(freeOwner, paidOwner, expiredOwner);
        await context.SaveChangesAsync();

        context.Subscriptions.AddRange(
            Subscription.CreateFree(freeOwner.Id, ReferenceNow),
            Subscription.CreatePaid(
                paidOwner.Id,
                SubscriptionPlan.Pro(),
                SubscriptionPeriod.Create(Today, Today.AddMonths(1).AddDays(-1)),
                ReferenceNow),
            Subscription.CreatePaid(
                expiredOwner.Id,
                SubscriptionPlan.Max(),
                SubscriptionPeriod.Create(Today.AddMonths(-2), Today.AddMonths(-1)),
                ReferenceNow.AddMonths(-2)));

        await context.SaveChangesAsync();

        var repository = new AdminRepository(context);

        var summary = await repository.GetDashboardSummary(
            Today,
            ReferenceNow.AddDays(-30));

        Assert.Equal(3, summary.TotalUsers);
        Assert.Equal(3, summary.TotalOwners);
        Assert.Equal(2, summary.ActiveSubscriptions);
        Assert.Equal(1, summary.PaidSubscriptions);
    }

    private static BooklyDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<BooklyDbContext>()
            .UseSqlite(connection)
            .Options;

        return new BooklyDbContext(options, new NoOpDomainEventDispatcher());
    }

    private static User CreateOwner(string email)
    {
        return User.CreateOwner(
            PersonName.Create("Test", "Owner"),
            BOOKLY.Domain.SharedKernel.Email.Create(email),
            Password.FromHash("hashed-password"),
            ReferenceNow);
    }

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
