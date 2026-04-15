using AutoMapper;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services;
using BOOKLY.Application.Services.AppointmentAggregate;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Domain.Emailing;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Queries;
using BOOKLY.Domain.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;

namespace BOOKLY.Infrastructure.Tests;

public sealed class AppointmentSearchCreatedByTests
{
    private static readonly DateTime ReferenceNow = new(2026, 4, 8, 10, 0, 0);

    [Fact]
    public async Task SearchAppointments_ShouldExposeCreatorActorForHistoryRows()
    {
        var owner = CreateOwner(10, "Ada", "Lovelace");
        var secretary = CreateSecretary(20, "Hedy", "Lamarr");

        var ownerCreatedAppointment = CreateAppointment(1, new DateTime(2026, 4, 10, 9, 0, 0), "Grace Hopper");
        SetEntityId(ownerCreatedAppointment, 1001);

        var secretaryCreatedAppointment = CreateAppointment(1, new DateTime(2026, 4, 10, 10, 0, 0), "Katherine Johnson");
        SetEntityId(secretaryCreatedAppointment, 1002);

        var clientCreatedAppointment = CreateAppointment(1, new DateTime(2026, 4, 10, 11, 0, 0), "Dorothy Vaughan");
        SetEntityId(clientCreatedAppointment, 1003);

        var appointmentRepository = new FakeAppointmentRepository(
        [
            ownerCreatedAppointment,
            secretaryCreatedAppointment,
            clientCreatedAppointment
        ]);

        var historyRepository = new FakeAppointmentHistoryRepository(
        [
            CreateCreationEntry(ownerCreatedAppointment.Id, owner, ReferenceNow.AddMinutes(1)),
            CreateCreationEntry(secretaryCreatedAppointment.Id, secretary, ReferenceNow.AddMinutes(2)),
            AppointmentStatusHistory.Create(
                clientCreatedAppointment.Id,
                null,
                AppointmentStatus.Pending,
                null,
                ReferenceNow.AddMinutes(3),
                null)
        ]);

        var sut = CreateSut(CreateService(), appointmentRepository, historyRepository);

        var result = await sut.SearchAppointments(new AppointmentQueryDto
        {
            ServiceId = 1
        });

        Assert.True(result.IsSuccess);
        var items = Assert.IsAssignableFrom<IReadOnlyCollection<AppointmentListItemDto>>(result.Data);

        var ownerRow = Assert.Single(items.Where(item => item.Id == ownerCreatedAppointment.Id));
        Assert.Equal(owner.Id, ownerRow.CreatedByUserId);
        Assert.Equal("Ada Lovelace", ownerRow.CreatedByUserDisplayName);
        Assert.Equal("Owner", ownerRow.CreatedByUserRole);

        var secretaryRow = Assert.Single(items.Where(item => item.Id == secretaryCreatedAppointment.Id));
        Assert.Equal(secretary.Id, secretaryRow.CreatedByUserId);
        Assert.Equal("Hedy Lamarr", secretaryRow.CreatedByUserDisplayName);
        Assert.Equal("Secretary", secretaryRow.CreatedByUserRole);

        var clientRow = Assert.Single(items.Where(item => item.Id == clientCreatedAppointment.Id));
        Assert.Null(clientRow.CreatedByUserId);
        Assert.Null(clientRow.CreatedByUserDisplayName);
        Assert.Null(clientRow.CreatedByUserRole);
    }

    private static AppointmentService CreateSut(
        Service service,
        FakeAppointmentRepository appointmentRepository,
        FakeAppointmentHistoryRepository historyRepository)
    {
        return new AppointmentService(
            appointmentRepository,
            new FakeServiceRepository(service),
            new FakeServiceTypeRepository(),
            new FakeUserRepository(),
            historyRepository,
            new AvailabilityService(),
            new FakeEmailService(),
            new FakeAppointmentCancellationNotificationService(),
            CreateMapper(),
            new FakeUnitOfWork(),
            new StubDateTimeProvider(),
            new ServiceAuthorizationService(),
            NullLogger<AppointmentService>.Instance);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddMaps(typeof(AppointmentService).Assembly));
        return configuration.CreateMapper();
    }

    private static Service CreateService()
    {
        var service = Service.Create(
            "Consulta general",
            10,
            "consulta-general",
            "Servicio de prueba",
            null,
            5,
            ReferenceNow,
            Duration.Create(60),
            Capacity.Create(1),
            Mode.Presence,
            1000m);

        SetEntityId(service, 1);

        return service;
    }

    private static Appointment CreateAppointment(int serviceId, DateTime startDateTime, string clientName)
    {
        var clientToken = clientName.Split(' ')[0].ToLowerInvariant();

        return Appointment.Create(
            serviceId,
            null,
            ClientInfo.Create(
                clientName,
                "1144455566",
                BOOKLY.Domain.SharedKernel.Email.Create($"{clientToken}@example.com")),
            startDateTime,
            Duration.Create(60),
            null,
            ReferenceNow);
    }

    private static User CreateOwner(int id, string firstName, string lastName)
    {
        var owner = User.CreateOwner(
            PersonName.Create(firstName, lastName),
            BOOKLY.Domain.SharedKernel.Email.Create($"{firstName.ToLowerInvariant()}@example.com"),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        SetEntityId(owner, id);
        return owner;
    }

    private static User CreateSecretary(int id, string firstName, string lastName)
    {
        var secretary = User.CreateSecretary(
            PersonName.Create(firstName, lastName),
            BOOKLY.Domain.SharedKernel.Email.Create($"{firstName.ToLowerInvariant()}@example.com"),
            ReferenceNow);

        SetEntityId(secretary, id);
        return secretary;
    }

    private static AppointmentStatusHistory CreateCreationEntry(int appointmentId, User user, DateTime occurredOn)
    {
        var entry = AppointmentStatusHistory.Create(
            appointmentId,
            null,
            AppointmentStatus.Pending,
            null,
            occurredOn,
            user.Id);

        typeof(AppointmentStatusHistory)
            .GetProperty(nameof(AppointmentStatusHistory.User))!
            .SetValue(entry, user);

        return entry;
    }

    private static void SetEntityId<T>(T entity, int id)
    {
        entity!.GetType().GetProperty("Id")!.SetValue(entity, id);
    }

    private sealed class FakeAppointmentRepository(IReadOnlyCollection<Appointment> appointments) : IAppointmentRepository
    {
        public Task<IReadOnlyCollection<Appointment>> GetByServiceAndDate(int serviceId, DateOnly date, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyCollection<Appointment>>([]);

        public Task<IReadOnlyCollection<Appointment>> GetByServiceAndDateRange(int serviceId, DateOnly from, DateOnly to, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyCollection<Appointment>>([]);

        public Task<List<Appointment>> GetPendingFutureByServiceAndDateRangeForUpdate(int serviceId, DateOnly from, DateOnly to, DateTime now, CancellationToken ct = default)
            => Task.FromResult(new List<Appointment>());

        public Task<IReadOnlyCollection<Appointment>> GetByService(int serviceId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyCollection<Appointment>>([]);

        public Task<IReadOnlyCollection<Appointment>> SearchByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly? from,
            DateOnly? to,
            AppointmentStatus? status,
            string? clientSearch,
            string? clientEmail,
            bool orderDescending,
            CancellationToken ct = default)
            => Task.FromResult(appointments);

        public Task<int> CountByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyCollection<AppointmentStatusCountResult>> GetStatusCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyCollection<AppointmentDayCountResult>> GetDayCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyCollection<AppointmentHourCountResult>> GetHourCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyCollection<AppointmentWeekdayCountResult>> GetWeekdayCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<bool> ExistsOverlap(int serviceId, DateTime start, DateTime end, int? excludedAppointmentId = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<Appointment?> GetOne(int id, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task AddOne(Appointment appointment, CancellationToken ct = default)
            => throw new NotImplementedException();

        public void Update(Appointment appointment)
            => throw new NotImplementedException();

        public void Remove(Appointment appointment)
            => throw new NotImplementedException();
    }

    private sealed class FakeAppointmentHistoryRepository(
        List<AppointmentStatusHistory> creationEntries) : IAppointmentHistoryRepository
    {
        public Task AddOne(AppointmentStatusHistory history, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<List<AppointmentStatusHistory>> GetByAppointment(int appointmentId, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<List<AppointmentStatusHistory>> GetByService(int serviceId, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<List<AppointmentStatusHistory>> GetCreationEntriesByAppointments(
            IReadOnlyCollection<int> appointmentIds,
            CancellationToken ct = default)
        {
            var result = creationEntries
                .Where(entry => appointmentIds.Contains(entry.AppointmentId))
                .ToList();

            return Task.FromResult(result);
        }
    }

    private sealed class FakeServiceRepository(Service service) : IServiceRepository
    {
        public Task<Service?> GetOne(int id, CancellationToken ct = default)
            => Task.FromResult<Service?>(id == service.Id ? service : null);

        public Task<Service?> GetBySlug(string slug, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetBySlugAndPublicBookingCode(string slug, string publicBookingCode, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSchedules(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithUnavailability(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSecretaries(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSchedulesAndUnavailability(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetBySlugWithSchedulesAndUnavailability(string slug, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetBySlugAndPublicBookingCodeWithSchedulesAndUnavailability(string slug, string publicBookingCode, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<BOOKLY.Domain.Aggregates.ServiceAggregate.Entities.ServiceSchedule>> GetSchedulesByService(int serviceId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<BOOKLY.Domain.Aggregates.ServiceAggregate.Entities.ServiceUnavailability>> GetUnavailabilityByService(int serviceId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Service>> GetServicesByOwner(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Service>> GetServicesByOwnerWithSecretaries(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<int>> GetServiceIdsBySecretary(int secretaryId, CancellationToken ct = default) => throw new NotImplementedException();
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

    private sealed class FakeServiceTypeRepository : IServiceTypeRepository
    {
        public Task<ICollection<ServiceType>> GetAll(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ServiceType?> GetByIdWithFields(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ServiceType?> GetOne(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddOne(ServiceType serviceType, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(ServiceType serviceType) => throw new NotImplementedException();
        public void Remove(ServiceType serviceType) => throw new NotImplementedException();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> GetOne(int id, CancellationToken ct = default) => Task.FromResult<User?>(null);
        public Task<User?> GetById(int id, CancellationToken ct = default) => GetOne(id, ct);
        public Task<User?> GetByEmail(string email, CancellationToken ct = default) => Task.FromResult<User?>(null);
        public Task<RefreshToken?> GetRefreshToken(string tokenHash, string? legacyRawToken = null, CancellationToken ct = default) => Task.FromResult<RefreshToken?>(null);
        public Task<bool> ExistsByEmail(string email, CancellationToken ct = default) => Task.FromResult(false);
        public Task AddOne(User user, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddRefreshToken(RefreshToken refreshToken, CancellationToken ct = default) => Task.CompletedTask;
        public Task RevokeAllUserTokens(int userId, CancellationToken ct = default) => Task.CompletedTask;
        public void Update(User user) => throw new NotImplementedException();
        public void Remove(User user) => throw new NotImplementedException();
    }

    private sealed class FakeEmailService : IEmailService
    {
        public Task SendEmailConfirmation(EmailConfirmationEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendPasswordReset(PasswordResetEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendSecretaryInvitation(SecretaryInvitationEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAdminInvitation(AdminInvitationEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCreatedToClient(AppointmentCreatedClientEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCreatedToOwner(AppointmentCreatedOwnerEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCancelledToClient(AppointmentCancelledClientEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentCancelledToOwner(AppointmentCancelledOwnerEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentRescheduledToClient(AppointmentRescheduledClientEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendAppointmentRescheduledToOwner(AppointmentRescheduledOwnerEmailModel model, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeAppointmentCancellationNotificationService : IAppointmentCancellationNotificationService
    {
        public Task NotifyAppointmentCancelled(Service service, Appointment appointment, bool notifyOwner, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChanges(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider
    {
        public DateTime NowArgentina() => ReferenceNow;
        public DateTime UtcNow() => ReferenceNow.ToUniversalTime();
    }
}
