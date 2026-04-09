using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Application.Services.PublicBooking;
using BOOKLY.Application.Services.PublicBooking.DTOs;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Queries;
using BOOKLY.Domain.Repositories;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Infrastructure.Tests;

public sealed class PublicBookingServiceTests
{
    private static readonly DateTime ReferenceNow = new(2026, 4, 8, 10, 0, 0);

    [Fact]
    public async Task AllPublicOperations_ShouldReturnSameNotFoundError_WhenTokenIsInvalid()
    {
        var service = CreateService();
        var appointmentService = new FakeAppointmentService();
        var sut = CreateSut(service, appointmentService: appointmentService);
        var invalidToken = "invalid-token";
        var date = DateOnly.FromDateTime(ReferenceNow.AddDays(1));

        var detailResult = await sut.GetService(service.Slug.Value, invalidToken);
        var datesResult = await sut.GetAvailableDates(service.Slug.Value, invalidToken, date, date.AddDays(7));
        var slotsResult = await sut.GetAvailableSlots(service.Slug.Value, invalidToken, date);
        var createResult = await sut.CreateAppointment(service.Slug.Value, invalidToken, CreateAppointmentRequest(date));

        AssertPublicError(detailResult.Error, ErrorType.NotFound, "Acceso publico invalido o inexistente.");
        AssertPublicError(datesResult.Error, ErrorType.NotFound, "Acceso publico invalido o inexistente.");
        AssertPublicError(slotsResult.Error, ErrorType.NotFound, "Acceso publico invalido o inexistente.");
        AssertPublicError(createResult.Error, ErrorType.NotFound, "Acceso publico invalido o inexistente.");
        Assert.Equal(0, appointmentService.CreateCalls);
    }

    [Fact]
    public async Task AllPublicOperations_ShouldReturnSameConflict_WhenPublicBookingIsDisabled()
    {
        var service = CreateService();
        service.DisablePublicBooking();

        var appointmentService = new FakeAppointmentService();
        var sut = CreateSut(service, appointmentService: appointmentService);
        var date = DateOnly.FromDateTime(ReferenceNow.AddDays(1));

        var detailResult = await sut.GetService(service.Slug.Value, service.PublicBookingToken);
        var datesResult = await sut.GetAvailableDates(service.Slug.Value, service.PublicBookingToken, date, date.AddDays(7));
        var slotsResult = await sut.GetAvailableSlots(service.Slug.Value, service.PublicBookingToken, date);
        var createResult = await sut.CreateAppointment(service.Slug.Value, service.PublicBookingToken, CreateAppointmentRequest(date));

        AssertPublicError(detailResult.Error, ErrorType.Conflict, "El acceso publico para este servicio esta deshabilitado.");
        AssertPublicError(datesResult.Error, ErrorType.Conflict, "El acceso publico para este servicio esta deshabilitado.");
        AssertPublicError(slotsResult.Error, ErrorType.Conflict, "El acceso publico para este servicio esta deshabilitado.");
        AssertPublicError(createResult.Error, ErrorType.Conflict, "El acceso publico para este servicio esta deshabilitado.");
        Assert.Equal(0, appointmentService.CreateCalls);
    }

    [Fact]
    public async Task CreateAppointment_ShouldRevalidateAccessAndDelegateToInternalAppointmentFlow_WhenAccessIsValid()
    {
        var service = CreateService();
        var date = DateOnly.FromDateTime(ReferenceNow.AddDays(1));
        var appointmentRequest = CreateAppointmentRequest(date);
        var appointmentService = new FakeAppointmentService
        {
            CreateResult = Result<AppointmentDto>.Success(new AppointmentDto
            {
                Id = 44,
                ServiceId = service.Id,
                ClientName = appointmentRequest.ClientName,
                ClientPhone = appointmentRequest.ClientPhone,
                ClientEmail = appointmentRequest.ClientEmail,
                StartDateTime = appointmentRequest.StartDateTime,
                EndDateTime = appointmentRequest.StartDateTime.AddMinutes(service.DurationMinutes.Value),
                DurationMinutes = service.DurationMinutes.Value,
                Status = AppointmentStatus.Pending.ToString(),
                FieldValues =
                [
                    new AppointmentFieldValueDto
                    {
                        FieldDefinitionId = 77,
                        Value = "prefers-window"
                    }
                ]
            })
        };

        var sut = CreateSut(service, appointmentService: appointmentService);

        var result = await sut.CreateAppointment(service.Slug.Value, service.PublicBookingToken, appointmentRequest);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, appointmentService.CreateCalls);
        Assert.NotNull(appointmentService.LastCreateAppointmentDto);
        Assert.Equal(service.Id, appointmentService.LastCreateAppointmentDto!.ServiceId);
        Assert.Equal(appointmentRequest.ClientName, appointmentService.LastCreateAppointmentDto.ClientName);
        Assert.Equal(appointmentRequest.ClientPhone, appointmentService.LastCreateAppointmentDto.ClientPhone);
        Assert.Equal(appointmentRequest.ClientEmail, appointmentService.LastCreateAppointmentDto.ClientEmail);
        Assert.Equal(appointmentRequest.StartDateTime, appointmentService.LastCreateAppointmentDto.StartDateTime);
        Assert.Single(appointmentService.LastCreateAppointmentDto.FieldValues);
        Assert.Equal(77, appointmentService.LastCreateAppointmentDto.FieldValues[0].FieldDefinitionId);
    }

    [Fact]
    public async Task GetService_ShouldIncludeOwnerName_WhenOwnerExists()
    {
        var service = CreateService();
        var sut = CreateSut(service);

        var result = await sut.GetService(service.Slug.Value, service.PublicBookingToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Ada Lovelace", result.Data!.OwnerName);
    }

    private static void AssertPublicError(Error error, ErrorType expectedType, string expectedMessage)
    {
        Assert.Equal(expectedType, error.Type);
        Assert.Equal(expectedMessage, error.Message);
    }

    private static PublicCreateAppointmentDto CreateAppointmentRequest(DateOnly date)
    {
        return new PublicCreateAppointmentDto
        {
            ClientName = "Grace Hopper",
            ClientPhone = "1199988877",
            ClientEmail = "grace@example.com",
            ClientNotes = "Window seat",
            StartDateTime = date.ToDateTime(new TimeOnly(9, 0)),
            FieldValues =
            [
                new PublicCreateAppointmentFieldValueDto
                {
                    FieldDefinitionId = 77,
                    Value = "prefers-window"
                }
            ]
        };
    }

    private static Service CreateService()
    {
        var service = Service.Create(
            "Consulta general",
            10,
            "consulta-general",
            "Servicio publico",
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
                Day.Create((int)ReferenceNow.AddDays(1).DayOfWeek))
        ]);

        return service;
    }

    private static PublicBookingService CreateSut(
        Service service,
        FakeAppointmentService? appointmentService = null)
    {
        return new PublicBookingService(
            new FakeServiceRepository(service),
            new FakeServiceTypeRepository(),
            new FakeUserRepository(),
            new FakeAppointmentRepository(),
            appointmentService ?? new FakeAppointmentService(),
            new FakeAvailabilityService(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeServiceRepository(Service service) : IServiceRepository
    {
        public Task<Service?> GetOne(int id, CancellationToken ct = default) => Task.FromResult(id == service.Id ? service : null);
        public Task<Service?> GetBySlug(string slug, CancellationToken ct = default) => Task.FromResult<Service?>(service);
        public Task<Service?> GetOneWithSchedules(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithUnavailability(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSecretaries(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetOneWithSchedulesAndUnavailability(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Service?> GetBySlugWithSchedulesAndUnavailability(string slug, CancellationToken ct = default) => Task.FromResult<Service?>(service);
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
        public Task<ServiceType?> GetByIdWithFields(int id, CancellationToken ct = default) => Task.FromResult<ServiceType?>(ServiceType.Create("Consultation"));
        public Task<ServiceType?> GetOne(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddOne(ServiceType serviceType, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(ServiceType serviceType) => throw new NotImplementedException();
        public void Remove(ServiceType serviceType) => throw new NotImplementedException();
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        public Task<IReadOnlyCollection<Appointment>> GetByServiceAndDate(int serviceId, DateOnly date, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyCollection<Appointment>>([]);

        public Task<IReadOnlyCollection<Appointment>> GetByServiceAndDateRange(int serviceId, DateOnly from, DateOnly to, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyCollection<Appointment>>([]);

        public Task<List<Appointment>> GetPendingFutureByServiceAndDateRangeForUpdate(int serviceId, DateOnly from, DateOnly to, DateTime now, CancellationToken ct = default)
            => Task.FromResult(new List<Appointment>());

        public Task<IReadOnlyCollection<Appointment>> GetByService(int serviceId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<Appointment>> SearchByServices(IReadOnlyCollection<int> serviceIds, DateOnly? from, DateOnly? to, AppointmentStatus? status, string? clientSearch, string? clientEmail, bool orderDescending, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<AppointmentStatusCountResult>> GetStatusCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<AppointmentDayCountResult>> GetDayCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<AppointmentHourCountResult>> GetHourCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<AppointmentWeekdayCountResult>> GetWeekdayCountsByServices(IReadOnlyCollection<int> serviceIds, DateOnly from, DateOnly to, int? secretaryId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsOverlap(int serviceId, DateTime start, DateTime end, int? excludedAppointmentId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Appointment?> GetOne(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddOne(Appointment appointment, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(Appointment appointment) => throw new NotImplementedException();
        public void Remove(Appointment appointment) => throw new NotImplementedException();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _owner = User.CreateOwner(
            PersonName.Create("Ada", "Lovelace"),
            BOOKLY.Domain.SharedKernel.Email.Create("ada@example.com"),
            Password.FromHash("hashed-password"),
            ReferenceNow);

        public FakeUserRepository()
        {
            _owner.Id = 10;
        }

        public Task<User?> GetOne(int id, CancellationToken ct = default)
            => Task.FromResult<User?>(id == _owner.Id ? _owner : null);

        public Task<User?> GetByEmail(string email, CancellationToken ct = default)
            => Task.FromResult<User?>(null);

        public Task<bool> ExistsByEmail(string email, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task AddOne(User user, CancellationToken ct = default) => throw new NotImplementedException();
        public void Update(User user) => throw new NotImplementedException();
        public void Remove(User user) => throw new NotImplementedException();
    }

    private sealed class FakeAppointmentService : IAppointmentService
    {
        public int CreateCalls { get; private set; }
        public CreateAppointmentDto? LastCreateAppointmentDto { get; private set; }
        public Result<AppointmentDto> CreateResult { get; init; } = Result<AppointmentDto>.Success(new AppointmentDto());

        public Task<Result<AppointmentDto>> CreateAppointment(CreateAppointmentDto dto, CancellationToken ct = default)
        {
            CreateCalls++;
            LastCreateAppointmentDto = dto;
            return Task.FromResult(CreateResult);
        }

        public Task<Result<AppointmentDto>> GetById(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<IReadOnlyCollection<AppointmentDto>>> GetByService(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<IReadOnlyCollection<AppointmentSummaryDto>>> GetByServiceAndDate(int id, DateOnly date, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<IReadOnlyCollection<AppointmentListItemDto>>> GetByDay(AppointmentDayQueryDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<IReadOnlyCollection<AppointmentListItemDto>>> SearchAppointments(AppointmentQueryDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>> GetHistoryByAppointment(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>> GetHistoryByService(int serviceId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<AppointmentDto>> UpdateAppointmentInformation(int id, UpdateAppointmentDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result<AppointmentDto>> RescheduleAppointment(int id, RescheduleAppointmentDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> MarkAsCancel(int id, CancelAppointmentDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> MarkAsAttended(int id, int? userId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Result> MarkAsNoShow(int id, int? userId = null, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeAvailabilityService : IAvailabilityService
    {
        public IReadOnlyCollection<DateTime> GetAvailableSlots(Service service, IReadOnlyCollection<Appointment> appointments, DateOnly date, DateTime now)
            => [date.ToDateTime(new TimeOnly(9, 0))];

        public IReadOnlyCollection<DateOnly> GetAvailableDates(Service service, IReadOnlyCollection<Appointment> appointments, DateOnly from, DateOnly to, DateTime now)
            => [from];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime NowArgentina() => ReferenceNow;
        public DateTime UtcNow() => ReferenceNow.ToUniversalTime();
    }
}
