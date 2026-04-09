using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services;
using BOOKLY.Application.Services.AppointmentAggregate;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
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

public sealed class AppointmentServiceTests
{
    private static readonly DateTime ReferenceNow = new(2026, 4, 8, 10, 0, 0);

    [Fact]
    public async Task CreateAppointment_ShouldRejectInactiveService()
    {
        var serviceDate = DateOnly.FromDateTime(ReferenceNow.AddDays(1));
        var service = CreateServiceWithSchedule(serviceDate);
        service.Deactivate();

        var appointmentRepository = new FakeAppointmentRepository();
        var sut = CreateSut(service, appointmentRepository);

        var result = await sut.CreateAppointment(new CreateAppointmentDto
        {
            ServiceId = service.Id,
            ClientName = "Grace Hopper",
            ClientPhone = "1144455566",
            ClientEmail = "grace@example.com",
            StartDateTime = serviceDate.ToDateTime(new TimeOnly(10, 0))
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("El servicio no se encuentra activo.", result.Error.Message);
        Assert.Equal(0, appointmentRepository.AddCalls);
    }

    [Fact]
    public async Task RescheduleAppointment_ShouldAllowInactiveService()
    {
        var serviceDate = DateOnly.FromDateTime(ReferenceNow.AddDays(1));
        var service = CreateServiceWithSchedule(serviceDate);
        service.Deactivate();

        var existingAppointment = Appointment.Create(
            service.Id,
            null,
            ClientInfo.Create("Grace Hopper", "1144455566", BOOKLY.Domain.SharedKernel.Email.Create("grace@example.com")),
            serviceDate.ToDateTime(new TimeOnly(9, 0)),
            Duration.Create(60),
            null,
            ReferenceNow);

        var appointmentRepository = new FakeAppointmentRepository(existingAppointment);
        var sut = CreateSut(service, appointmentRepository);
        var newStart = serviceDate.ToDateTime(new TimeOnly(10, 0));

        var result = await sut.RescheduleAppointment(
            1,
            new RescheduleAppointmentDto
            {
                StartDateTime = newStart
            });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(newStart, result.Data!.StartDateTime);
        Assert.True(appointmentRepository.WasUpdated);
    }

    private static AppointmentService CreateSut(Service service, FakeAppointmentRepository appointmentRepository)
    {
        return new AppointmentService(
            appointmentRepository,
            new FakeServiceRepository(service),
            new FakeServiceTypeRepository(),
            new FakeUserRepository(),
            new FakeAppointmentHistoryRepository(),
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

    private static Service CreateServiceWithSchedule(DateOnly date)
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

        service.SetSchedules(
        [
            ServiceSchedule.Create(
                TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(12, 0)),
                Capacity.Create(1),
                Day.Create((int)date.DayOfWeek))
        ]);

        typeof(Service)
            .GetProperty(nameof(Service.Id))!
            .SetValue(service, 1);

        return service;
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        private readonly Appointment? _appointment;

        public FakeAppointmentRepository(Appointment? appointment = null)
        {
            _appointment = appointment;
        }

        public int AddCalls { get; private set; }
        public bool WasUpdated { get; private set; }

        public Task<IReadOnlyCollection<Appointment>> GetByServiceAndDate(int serviceId, DateOnly date, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyCollection<Appointment>>([]);

        public Task<IReadOnlyCollection<Appointment>> GetByServiceAndDateRange(int serviceId, DateOnly from, DateOnly to, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyCollection<Appointment>>([]);

        public Task<List<Appointment>> GetPendingFutureByServiceAndDateRangeForUpdate(int serviceId, DateOnly from, DateOnly to, DateTime now, CancellationToken ct = default)
            => Task.FromResult(new List<Appointment>());

        public Task<IReadOnlyCollection<Appointment>> GetByService(int serviceId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyCollection<Appointment>>([]);

        public Task<IReadOnlyCollection<Appointment>> SearchByServices(IReadOnlyCollection<int> serviceIds, DateOnly? from, DateOnly? to, AppointmentStatus? status, string? clientSearch, string? clientEmail, bool orderDescending, CancellationToken ct = default)
            => throw new NotImplementedException();

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
            => Task.FromResult(false);

        public Task<Appointment?> GetOne(int id, CancellationToken ct = default)
            => Task.FromResult(_appointment);

        public Task AddOne(Appointment appointment, CancellationToken ct = default)
        {
            AddCalls++;
            return Task.CompletedTask;
        }

        public void Update(Appointment appointment)
        {
            WasUpdated = true;
        }

        public void Remove(Appointment appointment) => throw new NotImplementedException();
    }

    private sealed class FakeServiceRepository(Service service) : IServiceRepository
    {
        public Task<Service?> GetOne(int id, CancellationToken ct = default) => Task.FromResult<Service?>(service);
        public Task<Service?> GetBySlug(string slug, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSchedules(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithUnavailability(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSecretaries(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSchedulesAndUnavailability(int id, CancellationToken ct = default) => Task.FromResult<Service?>(service);
        public Task<Service?> GetBySlugWithSchedulesAndUnavailability(string slug, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ServiceSchedule>> GetSchedulesByService(int serviceId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ServiceUnavailability>> GetUnavailabilityByService(int serviceId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Service>> GetServicesByOwner(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Service>> GetServicesByOwnerWithSecretaries(int ownerId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<int>> GetServiceIdsBySecretary(int secretaryId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsSlug(string slug, int? excludedServiceId = null, CancellationToken ct = default) => throw new NotImplementedException();
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
        public Task<ServiceType?> GetByIdWithFields(int id, CancellationToken ct = default) => Task.FromResult<ServiceType?>(ServiceType.Create("Consulta"));
        public Task<ServiceType?> GetOne(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddOne(ServiceType serviceType, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(ServiceType serviceType) => throw new NotImplementedException();
        public void Remove(ServiceType serviceType) => throw new NotImplementedException();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> GetOne(int id, CancellationToken ct = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByEmail(string email, CancellationToken ct = default) => Task.FromResult<User?>(null);
        public Task<bool> ExistsByEmail(string email, CancellationToken ct = default) => Task.FromResult(false);
        public Task AddOne(User user, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(User user) => throw new NotImplementedException();
        public void Remove(User user) => throw new NotImplementedException();
    }

    private sealed class FakeAppointmentHistoryRepository : IAppointmentHistoryRepository
    {
        public Task AddOne(BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities.AppointmentStatusHistory history, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities.AppointmentStatusHistory>> GetByAppointment(int appointmentId, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<List<BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities.AppointmentStatusHistory>> GetByService(int serviceId, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<List<BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities.AppointmentStatusHistory>> GetCreationEntriesByAppointments(
            IReadOnlyCollection<int> appointmentIds,
            CancellationToken ct = default)
            => Task.FromResult(new List<BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities.AppointmentStatusHistory>());
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
