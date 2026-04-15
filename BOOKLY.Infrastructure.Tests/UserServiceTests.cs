using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.UserAggregate;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;
using BOOKLY.Domain.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;

namespace BOOKLY.Infrastructure.Tests;

public sealed class UserServiceTests
{
    private static readonly DateTime ReferenceNow = new(2026, 4, 8, 10, 0, 0);

    [Fact]
    public async Task Login_ShouldRejectInactiveUser()
    {
        var user = User.CreateOwner(
            PersonName.Create("Ada", "Lovelace"),
            BOOKLY.Domain.SharedKernel.Email.Create("ada@example.com"),
            Password.FromHash("valid-hash"),
            ReferenceNow);

        user.ConfirmEmail();
        user.Deactivate();

        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(user, unitOfWork);

        var result = await sut.Login(new LoginDto
        {
            Email = user.Email.Value,
            Password = "valid-password"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("La cuenta está desactivada.", result.Error.Message);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    private static UserService CreateSut(User user, FakeUnitOfWork unitOfWork)
    {
        return new UserService(
            new FakeUserRepository(user),
            new FakeServiceRepository(),
            new FakeSubscriptionRepository(),
            unitOfWork,
            new FakePasswordHasher(),
            new FakeTokenHashingService(),
            new FakeInvitationTokenGenerator(),
            new FakeUserTokenRepository(),
            new FakeEmailService(),
            new StubDateTimeProvider(),
            CreateMapper(),
            NullLogger<UserService>.Instance);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddMaps(typeof(UserService).Assembly));
        return configuration.CreateMapper();
    }

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public Task<User?> GetOne(int id, CancellationToken ct = default)
            => Task.FromResult<User?>(id == user.Id ? user : null);

        public Task<User?> GetById(int id, CancellationToken ct = default)
            => GetOne(id, ct);

        public Task<User?> GetByEmail(string email, CancellationToken ct = default)
            => Task.FromResult<User?>(string.Equals(email, user.Email.Value, StringComparison.OrdinalIgnoreCase) ? user : null);

        public Task<RefreshToken?> GetRefreshToken(string tokenHash, string? legacyRawToken = null, CancellationToken ct = default)
            => Task.FromResult<RefreshToken?>(null);

        public Task<bool> ExistsByEmail(string email, CancellationToken ct = default)
            => Task.FromResult(string.Equals(email, user.Email.Value, StringComparison.OrdinalIgnoreCase));

        public Task AddOne(User user, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddRefreshToken(RefreshToken refreshToken, CancellationToken ct = default) => Task.CompletedTask;
        public Task RevokeAllUserTokens(int userId, CancellationToken ct = default) => Task.CompletedTask;
        public void Update(User user) { }
        public void Remove(User user) => throw new NotImplementedException();
    }

    private sealed class FakeServiceRepository : IServiceRepository
    {
        public Task<Service?> GetOne(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetBySlug(string slug, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetBySlugAndPublicBookingCode(string slug, string publicBookingCode, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSchedules(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithUnavailability(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSecretaries(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSchedulesAndUnavailability(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetBySlugWithSchedulesAndUnavailability(string slug, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetBySlugAndPublicBookingCodeWithSchedulesAndUnavailability(string slug, string publicBookingCode, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ServiceSchedule>> GetSchedulesByService(int serviceId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ServiceUnavailability>> GetUnavailabilityByService(int serviceId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Service>> GetServicesByOwner(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Service>> GetServicesByOwnerWithSecretaries(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<int>> GetServiceIdsBySecretary(int secretaryId, CancellationToken ct = default) => Task.FromResult(new List<int>());
        public Task<List<int>> GetOwnerIdsBySecretary(int secretaryId, CancellationToken ct = default) => Task.FromResult(new List<int>());
        public Task<bool> ExistsSlug(string slug, int? excludedServiceId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsPublicBookingCode(string publicBookingCode, int? excludedServiceId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsBlock(int id, DateTime startDateTime, DateTime endDateTime, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountByOwnerId(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountActiveByOwnerId(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountAssignedSecretariesByOwnerId(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddOne(Service service, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(Service service) => throw new NotImplementedException();
        public void Remove(Service service) => throw new NotImplementedException();
    }

    private sealed class FakeSubscriptionRepository : ISubscriptionRepository
    {
        public Task<Subscription?> GetByOwnerId(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Subscription?> GetByOwnerIdForUpdate(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddOne(Subscription subscription, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(Subscription subscription) => throw new NotImplementedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChanges(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string plaintTextPassword) => $"{plaintTextPassword}-hash";

        public bool Verify(string plaintTextPassword, string passwordHash)
            => plaintTextPassword == "valid-password" && passwordHash == "valid-hash";
    }

    private sealed class FakeUserTokenRepository : IUserTokenRepository
    {
        public Task AddOne(UserToken userToken, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserToken?> GetByTokenHash(string tokenHash, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(UserToken token) => throw new NotImplementedException();
    }

    private sealed class FakeInvitationTokenGenerator : IInvitationTokenGenerator
    {
        public string GenerateToken() => "token";
    }

    private sealed class FakeTokenHashingService : ITokenHashingService
    {
        public string HashToken(string token) => token;
    }

    private sealed class FakeEmailService : IEmailService
    {
        public Task SendEmailConfirmation(BOOKLY.Domain.Emailing.EmailConfirmationEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendPasswordReset(BOOKLY.Domain.Emailing.PasswordResetEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendSecretaryInvitation(BOOKLY.Domain.Emailing.SecretaryInvitationEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAdminInvitation(BOOKLY.Domain.Emailing.AdminInvitationEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCreatedToClient(BOOKLY.Domain.Emailing.AppointmentCreatedClientEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCreatedToOwner(BOOKLY.Domain.Emailing.AppointmentCreatedOwnerEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCancelledToClient(BOOKLY.Domain.Emailing.AppointmentCancelledClientEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCancelledToOwner(BOOKLY.Domain.Emailing.AppointmentCancelledOwnerEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentRescheduledToClient(BOOKLY.Domain.Emailing.AppointmentRescheduledClientEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentRescheduledToOwner(BOOKLY.Domain.Emailing.AppointmentRescheduledOwnerEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider
    {
        public DateTime NowArgentina() => ReferenceNow;

        public DateTime UtcNow() => ReferenceNow.ToUniversalTime();
    }
}
