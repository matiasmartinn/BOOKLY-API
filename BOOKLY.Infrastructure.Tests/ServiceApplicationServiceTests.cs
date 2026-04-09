using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services;
using BOOKLY.Application.Services.ServiceAggregate;
using BOOKLY.Application.Services.ServiceAggregate.DTOs;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Queries;
using BOOKLY.Domain.Repositories;
using BOOKLY.Domain.SharedKernel;
using BOOKLY.Infrastructure.Email;
using Microsoft.Extensions.Options;

namespace BOOKLY.Infrastructure.Tests;

public sealed class ServiceApplicationServiceTests
{
    private static readonly DateTime ReferenceNow = new(2026, 4, 8, 10, 0, 0);

    [Fact]
    public async Task CreateService_ShouldIgnoreInactiveServicesWhenCheckingPlanLimits()
    {
        var owner = CreateConfirmedOwner("ada@example.com");
        var serviceType = ServiceType.Create("Consulta");
        var serviceRepository = new FakeServiceRepository(activeServiceCount: 0);
        var sut = CreateSut(
            serviceRepository,
            new FakeUserRepository(owner),
            new FakeServiceTypeRepository(serviceType),
            subscriptionRepository: new FakeSubscriptionRepository(null));

        var result = await sut.CreateService(CreateServiceDto(owner.Id, serviceType.Id, "Nuevo servicio"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Nuevo servicio", result.Data!.Name);
        Assert.NotNull(serviceRepository.AddedService);
    }

    [Fact]
    public async Task CreateService_ShouldRejectDisabledOwner()
    {
        var owner = CreateConfirmedOwner("grace@example.com");
        owner.Deactivate();

        var serviceType = ServiceType.Create("Consulta");
        var serviceRepository = new FakeServiceRepository(activeServiceCount: 0);
        var sut = CreateSut(
            serviceRepository,
            new FakeUserRepository(owner),
            new FakeServiceTypeRepository(serviceType),
            subscriptionRepository: new FakeSubscriptionRepository(null));

        var result = await sut.CreateService(CreateServiceDto(owner.Id, serviceType.Id, "Servicio bloqueado"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("El owner debe estar habilitado para crear servicios.", result.Error.Message);
        Assert.Null(serviceRepository.AddedService);
    }

    private static CreateServiceDto CreateServiceDto(int ownerId, int serviceTypeId, string name)
    {
        return new CreateServiceDto
        {
            Name = name,
            OwnerId = ownerId,
            ServiceTypeId = serviceTypeId,
            DurationMinutes = 60,
            Capacity = 1,
            Price = 1000m,
            Schedules =
            [
                new CreateServiceScheduleDto
                {
                    Day = 1,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(12, 0),
                    Capacity = 1
                }
            ]
        };
    }

    private static User CreateConfirmedOwner(string email)
    {
        var owner = User.CreateOwner(
            PersonName.Create("Ada", "Lovelace"),
            BOOKLY.Domain.SharedKernel.Email.Create(email),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        owner.ConfirmEmail();
        owner.Id = 10;
        return owner;
    }

    private static ServiceApplicationService CreateSut(
        FakeServiceRepository serviceRepository,
        FakeUserRepository userRepository,
        FakeServiceTypeRepository serviceTypeRepository,
        FakeSubscriptionRepository subscriptionRepository)
    {
        return new ServiceApplicationService(
            serviceRepository,
            new FakeAppointmentRepository(),
            new FakeAvailabilityService(),
            userRepository,
            serviceTypeRepository,
            subscriptionRepository,
            new StubDateTimeProvider(),
            new ServiceAuthorizationService(),
            new FakeAppointmentCancellationNotificationService(),
            CreateMapper(),
            new FakeUnitOfWork(),
            Options.Create(new FrontendOptions()));
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddMaps(typeof(ServiceApplicationService).Assembly));
        return configuration.CreateMapper();
    }

    private sealed class FakeServiceRepository : IServiceRepository
    {
        private readonly int _activeServiceCount;

        public FakeServiceRepository(int activeServiceCount)
        {
            _activeServiceCount = activeServiceCount;
        }

        public Service? AddedService { get; private set; }

        public Task<Service?> GetOne(int id, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<Service?> GetBySlug(string slug, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<Service?> GetOneWithSchedules(int id, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<Service?> GetOneWithUnavailability(int id, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<Service?> GetOneWithSecretaries(int id, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<Service?> GetOneWithSchedulesAndUnavailability(int id, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<Service?> GetBySlugWithSchedulesAndUnavailability(string slug, CancellationToken ct = default) => Task.FromResult<Service?>(null);
        public Task<List<ServiceSchedule>> GetSchedulesByService(int serviceId, CancellationToken ct = default) => Task.FromResult(new List<ServiceSchedule>());
        public Task<List<ServiceUnavailability>> GetUnavailabilityByService(int serviceId, CancellationToken ct = default) => Task.FromResult(new List<ServiceUnavailability>());
        public Task<List<Service>> GetServicesByOwner(int ownerId, CancellationToken ct = default) => Task.FromResult(new List<Service>());
        public Task<List<Service>> GetServicesByOwnerWithSecretaries(int ownerId, CancellationToken ct = default) => Task.FromResult(new List<Service>());
        public Task<List<int>> GetServiceIdsBySecretary(int secretaryId, CancellationToken ct = default) => Task.FromResult(new List<int>());
        public Task<bool> ExistsSlug(string slug, int? excludedServiceId = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> ExistsBlock(int id, DateTime startDateTime, DateTime endDateTime, CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> CountByOwnerId(int ownerId, CancellationToken ct = default) => Task.FromResult(_activeServiceCount);
        public Task<int> CountActiveByOwnerId(int ownerId, CancellationToken ct = default) => Task.FromResult(_activeServiceCount);
        public Task<int> CountAssignedSecretariesByOwnerId(int ownerId, CancellationToken ct = default) => Task.FromResult(0);

        public Task AddOne(Service service, CancellationToken ct = default)
        {
            AddedService = service;
            return Task.CompletedTask;
        }

        public void Update(Service service) => AddedService = service;
        public void Remove(Service service) => throw new NotImplementedException();
    }

    private sealed class FakeUserRepository(User owner) : IUserRepository
    {
        public Task<User?> GetOne(int id, CancellationToken ct = default)
            => Task.FromResult<User?>(id == owner.Id ? owner : null);

        public Task<User?> GetByEmail(string email, CancellationToken ct = default) => Task.FromResult<User?>(null);
        public Task<bool> ExistsByEmail(string email, CancellationToken ct = default) => Task.FromResult(false);
        public Task AddOne(User user, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(User user) => throw new NotImplementedException();
        public void Remove(User user) => throw new NotImplementedException();
    }

    private sealed class FakeServiceTypeRepository(ServiceType serviceType) : IServiceTypeRepository
    {
        public Task<ICollection<ServiceType>> GetAll(CancellationToken ct = default) => Task.FromResult<ICollection<ServiceType>>([serviceType]);
        public Task<ServiceType?> GetByIdWithFields(int id, CancellationToken ct = default) => Task.FromResult<ServiceType?>(id == serviceType.Id ? serviceType : null);
        public Task<ServiceType?> GetOne(int id, CancellationToken ct = default) => Task.FromResult<ServiceType?>(id == serviceType.Id ? serviceType : null);
        public Task AddOne(ServiceType serviceType, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(ServiceType serviceType) => throw new NotImplementedException();
        public void Remove(ServiceType serviceType) => throw new NotImplementedException();
    }

    private sealed class FakeSubscriptionRepository(Subscription? subscription) : ISubscriptionRepository
    {
        public Task<Subscription?> GetByOwnerId(int ownerId, CancellationToken ct = default) => Task.FromResult(subscription);
        public Task<Subscription?> GetByOwnerIdForUpdate(int ownerId, CancellationToken ct = default) => Task.FromResult(subscription);
        public Task AddOne(Subscription subscription, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(Subscription subscription) => throw new NotImplementedException();
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        public Task<IReadOnlyCollection<Appointment>> GetByServiceAndDate(int serviceId, DateOnly date, CancellationToken ct = default) => Task.FromResult<IReadOnlyCollection<Appointment>>([]);
        public Task<IReadOnlyCollection<Appointment>> GetByServiceAndDateRange(int serviceId, DateOnly from, DateOnly to, CancellationToken ct = default) => Task.FromResult<IReadOnlyCollection<Appointment>>([]);
        public Task<List<Appointment>> GetPendingFutureByServiceAndDateRangeForUpdate(int serviceId, DateOnly from, DateOnly to, DateTime now, CancellationToken ct = default) => Task.FromResult(new List<Appointment>());
        public Task<IReadOnlyCollection<Appointment>> GetByService(int serviceId, CancellationToken ct = default) => Task.FromResult<IReadOnlyCollection<Appointment>>([]);
        public Task<IReadOnlyCollection<Appointment>> SearchByServices(IReadOnlyCollection<int> serviceIds, DateOnly? from, DateOnly? to, AppointmentStatus? status, string? clientSearch, string? clientEmail, bool orderDescending, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<AppointmentStatusCountResult>> GetStatusCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<AppointmentDayCountResult>> GetDayCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<AppointmentHourCountResult>> GetHourCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<AppointmentWeekdayCountResult>> GetWeekdayCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsOverlap(int serviceId, DateTime start, DateTime end, int? excludedAppointmentId = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task<Appointment?> GetOne(int id, CancellationToken ct = default) => Task.FromResult<Appointment?>(null);
        public Task AddOne(Appointment appointment, CancellationToken ct = default) => Task.CompletedTask;
        public void Update(Appointment appointment) { }
        public void Remove(Appointment appointment) => throw new NotImplementedException();
    }

    private sealed class FakeAvailabilityService : IAvailabilityService
    {
        public IReadOnlyCollection<DateTime> GetAvailableSlots(Service service, IReadOnlyCollection<Appointment> appointments, DateOnly date, DateTime now) => [];
        public IReadOnlyCollection<DateOnly> GetAvailableDates(Service service, IReadOnlyCollection<Appointment> appointments, DateOnly from, DateOnly to, DateTime now) => [];
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
