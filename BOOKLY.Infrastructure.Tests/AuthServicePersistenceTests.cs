using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AuthAggregate;
using BOOKLY.Application.Services.AuthAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Infrastructure.Persistence;
using BOOKLY.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BOOKLY.Infrastructure.Tests;

public sealed class AuthServicePersistenceTests
{
    [Fact]
    public async Task Login_ShouldPersistLastLoginAtAsUnspecified_WhenProviderReturnsUtcKind()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection().BuildServiceProvider();
        var options = new DbContextOptionsBuilder<BooklyDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new BooklyDbContext(options, new DomainEventDispatcher(services));
        await context.Database.EnsureCreatedAsync();

        var user = User.CreateOwner(
            PersonName.Create("Ada", "Lovelace"),
            BOOKLY.Domain.SharedKernel.Email.Create("ada@example.com"),
            Password.FromHash("valid-hash"),
            new DateTime(2026, 3, 27, 10, 0, 0, DateTimeKind.Unspecified));
        user.ConfirmEmail();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var sut = new AuthService(
            new UserRepository(context),
            new FakePasswordHasher(),
            new UtcKindDateTimeProvider(),
            new FakeTokenHashingService(),
            new FakeJwtTokenService(),
            context);

        var result = await sut.Login(new LoginRequest
        {
            Email = user.Email.Value,
            Password = "valid-password"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(user.LastLoginAt);
        Assert.Equal(DateTimeKind.Unspecified, user.LastLoginAt.Value.Kind);

        var refreshToken = await context.RefreshTokens.SingleAsync();

        Assert.Equal(DateTimeKind.Utc, refreshToken.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, refreshToken.ExpiresAt.Kind);
    }

    private sealed class UtcKindDateTimeProvider : IDateTimeProvider
    {
        private static readonly DateTime ReferenceNow = new(2026, 3, 27, 13, 0, 0, DateTimeKind.Utc);

        public DateTime NowArgentina() => ReferenceNow;

        public DateTime UtcNow() => ReferenceNow;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string plaintTextPassword) => $"{plaintTextPassword}-hash";

        public bool Verify(string plaintTextPassword, string passwordHash)
            => plaintTextPassword == "valid-password" && passwordHash == "valid-hash";
    }

    private sealed class FakeTokenHashingService : ITokenHashingService
    {
        public string HashToken(string token) => token;
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string GenerateAccessToken(User user) => $"access-token-{user.Id}";
    }
}
