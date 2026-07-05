using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Tests;

public sealed class UnitOfWorkTransactionTests
{
    private static readonly DateTime ReferenceNow = new(2026, 4, 8, 10, 0, 0);

    [Fact]
    public async Task ExecuteInTransaction_ShouldRollbackFirstSave_WhenSecondStepFails()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var user = CreateOwner("ada@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.ExecuteInTransaction<bool>(async () =>
            {
                context.Users.Add(user);
                await context.SaveChanges();

                throw new InvalidOperationException("Falla simulada despues del primer save.");
            }));

        Assert.Equal(0, await context.Users.CountAsync());
    }

    [Fact]
    public async Task ExecuteInTransaction_ShouldPersistBothSaves_WhenOperationSucceeds()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var user = CreateOwner("ada@example.com");

        var token = await context.ExecuteInTransaction(async () =>
        {
            context.Users.Add(user);
            await context.SaveChanges();

            var userToken = UserToken.Create(
                user.Id,
                UserTokenPurpose.EmailConfirmation,
                "token-hash",
                ReferenceNow,
                TimeSpan.FromHours(24));

            context.UserTokens.Add(userToken);
            await context.SaveChanges();
            return userToken;
        });

        Assert.True(user.Id > 0);
        Assert.Equal(user.Id, token.UserId);
        Assert.Equal(1, await context.Users.CountAsync());
        Assert.Equal(1, await context.UserTokens.CountAsync());
    }

    [Fact]
    public async Task SaveChanges_ShouldStillCommitOnItsOwn_WhenThereIsNoAmbientTransaction()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        context.Users.Add(CreateOwner("ada@example.com"));
        await context.SaveChanges();

        Assert.Equal(1, await context.Users.CountAsync());
    }

    private static BooklyDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<BooklyDbContext>()
            .UseSqlite(connection)
            .Options;

        return new BooklyDbContext(options, new NoOpDomainEventDispatcher());
    }

    private static User CreateOwner(string email)
        => User.CreateOwner(
            PersonName.Create("Ada", "Lovelace"),
            BOOKLY.Domain.SharedKernel.Email.Create(email),
            Password.FromHash("hashed-password"),
            ReferenceNow);

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken ct = default) => Task.CompletedTask;
    }
}
