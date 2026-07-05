using AutoMapper;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services;
using BOOKLY.Application.Services.SubscriptionAggregate;
using BOOKLY.Application.Services.UserAggregate;
using BOOKLY.Application.Services.UserAggregate.SecretaryManagement;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Emailing;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;
using BOOKLY.Domain.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;

namespace BOOKLY.Infrastructure.Tests;

public sealed class SecretaryManagementServiceTests
{
    private static readonly DateTime ReferenceNow = new(2026, 4, 8, 10, 0, 0);

    [Fact]
    public async Task GetSecretariesByOwner_ShouldUseBatchUserLookup()
    {
        var owner = CreateOwner(10, "Ada", "Lovelace", "ada@example.com");
        var secretaryOne = CreateSecretary(20, "Hedy", "Lamarr", "hedy@example.com");
        var secretaryTwo = CreateSecretary(30, "Grace", "Hopper", "grace@example.com");
        var service = CreateService(100, owner.Id);
        service.AssignSecretary(secretaryOne.Id);
        service.AssignSecretary(secretaryTwo.Id);

        var userRepository = new FakeUserRepository(owner, secretaryOne, secretaryTwo);
        var serviceRepository = new FakeServiceRepository(service);
        var sut = CreateSut(userRepository, serviceRepository);

        var result = await sut.GetSecretariesByOwner(owner.Id);

        Assert.True(result.IsSuccess);
        var secretaries = Assert.IsAssignableFrom<IReadOnlyCollection<Application.Services.UserAggregate.DTOs.SecretaryDto>>(result.Data);
        Assert.Equal([20, 30], secretaries.Select(secretary => secretary.Id).OrderBy(id => id));
        Assert.All(secretaries, secretary => Assert.Equal([service.Id], secretary.ServiceIds));
        Assert.Equal(1, userRepository.GetByIdsCalls);
        Assert.Equal(1, userRepository.GetOneCalls);
    }

    [Fact]
    public async Task CreateSecretary_ShouldAssignServiceCreateTokenAndSendInvitation()
    {
        var owner = CreateOwner(10, "Ada", "Lovelace", "ada@example.com");
        var service = CreateService(100, owner.Id);
        var userRepository = new FakeUserRepository(owner);
        var serviceRepository = new FakeServiceRepository(service);
        var tokenRepository = new FakeUserTokenRepository();
        var emailService = new FakeEmailService();
        var unitOfWork = new FakeUnitOfWork();
        var sut = CreateSut(userRepository, serviceRepository, tokenRepository, emailService, unitOfWork);

        var result = await sut.CreateSecretary(
            owner.Id,
            new CreateSecretaryDto
            {
                FirstName = "Hedy",
                LastName = "Lamarr",
                Email = "hedy@example.com",
                ServiceId = service.Id
            });

        Assert.True(result.IsSuccess);
        Assert.NotNull(userRepository.AddedUser);
        Assert.Equal(UserRole.Secretary, userRepository.AddedUser!.Role);
        Assert.Contains(userRepository.AddedUser.Id, service.SecretaryIds);
        var token = Assert.Single(tokenRepository.AddedTokens);
        Assert.Equal(userRepository.AddedUser.Id, token.UserId);
        Assert.Equal(UserTokenPurpose.SecretaryInvitation, token.Purpose);
        Assert.True(emailService.SecretaryInvitationSent);
        Assert.Equal("raw-token", emailService.SecretaryInvitationToken);
        Assert.Equal(2, unitOfWork.SaveChangesCalls);
    }

    private static SecretaryManagementService CreateSut(
        FakeUserRepository userRepository,
        FakeServiceRepository serviceRepository,
        FakeUserTokenRepository? tokenRepository = null,
        FakeEmailService? emailService = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        var mapper = CreateMapper();
        var resolvedUnitOfWork = unitOfWork ?? new FakeUnitOfWork();
        var resolvedEmailService = emailService ?? new FakeEmailService();
        var dateTimeProvider = new StubDateTimeProvider();
        var userTokenIssuer = new UserTokenIssuer(
            tokenRepository ?? new FakeUserTokenRepository(),
            new FakeInvitationTokenGenerator(),
            new FakeTokenHashingService(),
            dateTimeProvider);
        var userDtoMapper = new UserDtoMapper(serviceRepository, mapper);
        var safeEmailDispatcher = new SafeEmailDispatcher(NullLogger<SafeEmailDispatcher>.Instance);
        var userProfileUpdateService = new UserProfileUpdateService(
            userRepository,
            resolvedUnitOfWork,
            userTokenIssuer,
            userDtoMapper,
            safeEmailDispatcher,
            resolvedEmailService);

        return new SecretaryManagementService(
            userRepository,
            serviceRepository,
            resolvedUnitOfWork,
            userTokenIssuer,
            userProfileUpdateService,
            userDtoMapper,
            safeEmailDispatcher,
            new FakeEffectiveSubscriptionResolver(),
            resolvedEmailService,
            dateTimeProvider,
            mapper);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddMaps(typeof(SecretaryManagementService).Assembly));
        return configuration.CreateMapper();
    }

    private static User CreateOwner(int id, string firstName, string lastName, string email)
    {
        var owner = User.CreateOwner(
            PersonName.Create(firstName, lastName),
            BOOKLY.Domain.SharedKernel.Email.Create(email),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        owner.Id = id;
        return owner;
    }

    private static User CreateSecretary(int id, string firstName, string lastName, string email)
    {
        var secretary = User.CreateSecretary(
            PersonName.Create(firstName, lastName),
            BOOKLY.Domain.SharedKernel.Email.Create(email),
            ReferenceNow);

        secretary.Id = id;
        return secretary;
    }

    private static Service CreateService(int id, int ownerId)
    {
        var service = Service.Create(
            "Consulta general",
            ownerId,
            $"consulta-{id}",
            "Servicio de prueba",
            null,
            1,
            ReferenceNow,
            Duration.Create(60),
            Capacity.Create(1),
            Mode.Presence,
            1000m,
            "FIXED123");

        typeof(Service).GetProperty(nameof(Service.Id))!.SetValue(service, id);
        return service;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<int, User> _users;
        private int _nextId = 1000;

        public FakeUserRepository(params User[] users)
        {
            _users = users.ToDictionary(user => user.Id);
        }

        public User? AddedUser { get; private set; }
        public int GetOneCalls { get; private set; }
        public int GetByIdsCalls { get; private set; }

        public Task<User?> GetOne(int id, CancellationToken ct = default)
        {
            GetOneCalls++;
            _users.TryGetValue(id, out var user);
            return Task.FromResult<User?>(user);
        }

        public Task<User?> GetById(int id, CancellationToken ct = default)
            => GetOne(id, ct);

        public Task<IReadOnlyCollection<User>> GetByIds(IReadOnlyCollection<int> ids, CancellationToken ct = default)
        {
            GetByIdsCalls++;
            return Task.FromResult<IReadOnlyCollection<User>>(
                ids.Where(_users.ContainsKey).Select(id => _users[id]).ToList());
        }

        public Task<User?> GetByEmail(string email, CancellationToken ct = default)
        {
            var user = _users.Values.FirstOrDefault(current =>
                string.Equals(current.Email.Value, email.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(user);
        }

        public Task<RefreshToken?> GetRefreshToken(string tokenHash, CancellationToken ct = default)
            => Task.FromResult<RefreshToken?>(null);

        public Task<bool> ExistsByEmail(string email, CancellationToken ct = default)
            => Task.FromResult(_users.Values.Any(user =>
                string.Equals(user.Email.Value, email.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)));

        public Task AddOne(User user, CancellationToken ct = default)
        {
            user.Id = ++_nextId;
            AddedUser = user;
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task AddRefreshToken(RefreshToken refreshToken, CancellationToken ct = default) => Task.CompletedTask;
        public Task RevokeAllUserTokens(int userId, CancellationToken ct = default) => Task.CompletedTask;
        public void Update(User user) => _users[user.Id] = user;
        public void Remove(User user) => _users.Remove(user.Id);
    }

    private sealed class FakeServiceRepository(Service service) : IServiceRepository
    {
        public Task<Service?> GetOne(int id, CancellationToken ct = default)
            => Task.FromResult<Service?>(id == service.Id ? service : null);

        public Task<List<Service>> GetServicesByOwnerWithSecretaries(int ownerId, CancellationToken ct = default)
            => Task.FromResult(service.OwnerId == ownerId ? [service] : new List<Service>());

        public Task<List<int>> GetServiceIdsBySecretary(int secretaryId, CancellationToken ct = default)
            => Task.FromResult(service.SecretaryIds.Contains(secretaryId) ? [service.Id] : new List<int>());

        public Task<List<int>> GetOwnerIdsBySecretary(int secretaryId, CancellationToken ct = default)
            => Task.FromResult(service.SecretaryIds.Contains(secretaryId) ? [service.OwnerId] : new List<int>());

        public Task<int> CountAssignedSecretariesByOwnerId(int ownerId, CancellationToken ct = default)
            => Task.FromResult(service.OwnerId == ownerId ? service.SecretaryIds.Distinct().Count() : 0);

        public void Update(Service service) { }

        public Task<Service?> GetBySlug(string slug, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetBySlugAndPublicBookingCode(string slug, string publicBookingCode, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSchedules(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithUnavailability(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSecretaries(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSchedulesAndUnavailability(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSchedulesAndUnavailabilityForUpdate(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetBySlugWithSchedulesAndUnavailability(string slug, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetBySlugAndPublicBookingCodeWithSchedulesAndUnavailability(string slug, string publicBookingCode, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ServiceSchedule>> GetSchedulesByService(int serviceId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ServiceUnavailability>> GetUnavailabilityByService(int serviceId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Service>> GetServicesByOwner(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsSlug(string slug, int? excludedServiceId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsPublicBookingCode(string publicBookingCode, int? excludedServiceId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsBlock(int id, DateTime startDateTime, DateTime endDateTime, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountByOwnerId(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountActiveByOwnerId(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddOne(Service service, CancellationToken ct = default) => throw new NotImplementedException();
        public void Remove(Service service) => throw new NotImplementedException();
    }

    private sealed class FakeUserTokenRepository : IUserTokenRepository
    {
        public List<UserToken> AddedTokens { get; } = [];

        public Task AddOne(UserToken userToken, CancellationToken ct = default)
        {
            AddedTokens.Add(userToken);
            return Task.CompletedTask;
        }

        public Task<UserToken?> GetByTokenHash(string tokenHash, CancellationToken ct = default) => Task.FromResult<UserToken?>(null);
        public void Update(UserToken token) { }
    }

    private sealed class FakeInvitationTokenGenerator : IInvitationTokenGenerator
    {
        public string GenerateToken() => "raw-token";
    }

    private sealed class FakeTokenHashingService : ITokenHashingService
    {
        public string HashToken(string token) => $"hash:{token}";
    }

    private sealed class FakeEffectiveSubscriptionResolver : IEffectiveSubscriptionResolver
    {
        public Task<Subscription> Resolve(int ownerId, CancellationToken ct = default)
            => Task.FromResult(Subscription.CreatePaid(
                ownerId,
                SubscriptionPlan.Pro(),
                SubscriptionPeriod.Create(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30)),
                ReferenceNow));
    }

    private sealed class FakeEmailService : IEmailService
    {
        public bool SecretaryInvitationSent { get; private set; }
        public string? SecretaryInvitationToken { get; private set; }

        public Task SendSecretaryInvitation(SecretaryInvitationEmailModel model, CancellationToken ct = default)
        {
            SecretaryInvitationSent = true;
            SecretaryInvitationToken = model.Token;
            return Task.CompletedTask;
        }

        public Task SendEmailConfirmation(EmailConfirmationEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendPasswordReset(PasswordResetEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAdminInvitation(AdminInvitationEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCreatedToClient(AppointmentCreatedClientEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCreatedToOwner(AppointmentCreatedOwnerEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCancelledToClient(AppointmentCancelledClientEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCancelledToOwner(AppointmentCancelledOwnerEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentRescheduledToClient(AppointmentRescheduledClientEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentRescheduledToOwner(AppointmentRescheduledOwnerEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChanges(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }

        public Task<T> ExecuteInTransaction<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
            => operation();
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider
    {
        public DateTime NowArgentina() => ReferenceNow;
        public DateTime UtcNow() => ReferenceNow.ToUniversalTime();
    }
}
