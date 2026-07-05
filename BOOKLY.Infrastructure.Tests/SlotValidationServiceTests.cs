using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.SlotValidationService;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Queries;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Infrastructure.Tests;

public sealed class SlotValidationServiceTests
{
    private static readonly DateTime ReferenceNow = new(2026, 7, 6, 8, 0, 0);

    [Fact]
    public async Task ValidateSlotAvailability_ShouldRejectInactiveService_WhenActiveServiceIsRequired()
    {
        var requestedStart = ReferenceNow.AddHours(1);
        var service = CreateServiceWithSchedule(requestedStart);
        service.Deactivate();
        var repository = new FakeAppointmentRepository([]);
        var availabilityService = new FakeAvailabilityService([requestedStart]);
        var sut = CreateSut(repository, availabilityService);

        var result = await sut.ValidateSlotAvailability(
            service,
            requestedStart,
            excludedAppointmentId: null,
            requireActiveService: true);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("El servicio no se encuentra activo.", result.Error.Message);
        Assert.Equal(0, repository.GetByServiceAndDateCallCount);
        Assert.Equal(0, availabilityService.GetAvailableSlotsCallCount);
    }

    [Fact]
    public async Task ValidateSlotAvailability_ShouldAllowInactiveService_WhenRescheduling()
    {
        var requestedStart = ReferenceNow.AddHours(1);
        var service = CreateServiceWithSchedule(requestedStart);
        service.Deactivate();
        var repository = new FakeAppointmentRepository([]);
        var availabilityService = new FakeAvailabilityService([requestedStart]);
        var sut = CreateSut(repository, availabilityService);

        var result = await sut.ValidateSlotAvailability(
            service,
            requestedStart,
            excludedAppointmentId: null,
            requireActiveService: false);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateSlotAvailability_ShouldRejectPastOrCurrentSlots()
    {
        var service = CreateServiceWithSchedule(ReferenceNow);
        var repository = new FakeAppointmentRepository([]);
        var availabilityService = new FakeAvailabilityService([ReferenceNow]);
        var sut = CreateSut(repository, availabilityService);

        var result = await sut.ValidateSlotAvailability(
            service,
            ReferenceNow,
            excludedAppointmentId: null,
            requireActiveService: true);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("El turno debe agendarse en un horario futuro.", result.Error.Message);
        Assert.Equal(0, repository.GetByServiceAndDateCallCount);
    }

    [Fact]
    public async Task ValidateSlotAvailability_ShouldRejectSlotsOutsideTheServiceSchedule()
    {
        var scheduledStart = ReferenceNow.AddHours(1);
        var requestedStart = scheduledStart.AddHours(2);
        var service = CreateServiceWithSchedule(scheduledStart);
        var repository = new FakeAppointmentRepository([]);
        var availabilityService = new FakeAvailabilityService([requestedStart]);
        var sut = CreateSut(repository, availabilityService);

        var result = await sut.ValidateSlotAvailability(
            service,
            requestedStart,
            excludedAppointmentId: null,
            requireActiveService: true);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("El horario no pertenece a la agenda del servicio.", result.Error.Message);
        Assert.Equal(0, repository.GetByServiceAndDateCallCount);
    }

    [Fact]
    public async Task ValidateSlotAvailability_ShouldExcludeCurrentAppointment_WhenRescheduling()
    {
        var requestedStart = ReferenceNow.AddHours(1);
        var service = CreateServiceWithSchedule(requestedStart);
        var excludedAppointment = CreateAppointment(id: 10, requestedStart);
        var otherAppointment = CreateAppointment(id: 20, requestedStart);
        var repository = new FakeAppointmentRepository([excludedAppointment, otherAppointment]);
        var availabilityService = new FakeAvailabilityService([requestedStart]);
        var sut = CreateSut(repository, availabilityService);

        var result = await sut.ValidateSlotAvailability(
            service,
            requestedStart,
            excludedAppointmentId: excludedAppointment.Id,
            requireActiveService: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(DateOnly.FromDateTime(requestedStart), repository.RequestedDate);
        var evaluatedAppointment = Assert.Single(availabilityService.LastAppointments);
        Assert.Equal(otherAppointment.Id, evaluatedAppointment.Id);
        Assert.Equal(ReferenceNow, availabilityService.LastNow);
    }

    [Fact]
    public async Task ValidateSlotAvailability_ShouldReturnConflict_WhenRequestedSlotIsUnavailable()
    {
        var requestedStart = ReferenceNow.AddHours(1);
        var service = CreateServiceWithSchedule(requestedStart);
        var repository = new FakeAppointmentRepository([]);
        var availabilityService = new FakeAvailabilityService([]);
        var sut = CreateSut(repository, availabilityService);

        var result = await sut.ValidateSlotAvailability(
            service,
            requestedStart,
            excludedAppointmentId: null,
            requireActiveService: true);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("El horario seleccionado ya no está disponible.", result.Error.Message);
    }

    private static SlotValidationService CreateSut(
        IAppointmentRepository appointmentRepository,
        IAvailabilityService availabilityService)
    {
        return new SlotValidationService(
            appointmentRepository,
            availabilityService,
            new StubDateTimeProvider());
    }

    private static Service CreateServiceWithSchedule(DateTime slotStart)
    {
        var service = Service.Create(
            "Consulta general",
            1,
            "consulta-general",
            "Servicio de prueba",
            null,
            1,
            ReferenceNow.AddDays(-1),
            Duration.Create(60),
            Capacity.Create(1),
            Mode.Presence,
            1000m);

        service.SetSchedules(
        [
            ServiceSchedule.Create(
                TimeRange.Create(
                    TimeOnly.FromDateTime(slotStart),
                    TimeOnly.FromDateTime(slotStart.AddHours(1))),
                Capacity.Create(1),
                Day.Create((int)slotStart.DayOfWeek))
        ]);

        return service;
    }

    private static Appointment CreateAppointment(int id, DateTime start)
    {
        var appointment = Appointment.CreateHistorical(
            1,
            null,
            ClientInfo.Create(
                "Grace Hopper",
                "1199988877",
                BOOKLY.Domain.SharedKernel.Email.Create("grace@example.com")),
            start,
            Duration.Create(60),
            AppointmentStatus.Pending,
            null,
            null,
            ReferenceNow);

        typeof(Appointment).GetProperty(nameof(Appointment.Id))!.SetValue(appointment, id);
        return appointment;
    }

    private sealed class FakeAvailabilityService(IReadOnlyCollection<DateTime> availableSlots) : IAvailabilityService
    {
        public int GetAvailableSlotsCallCount { get; private set; }
        public IReadOnlyCollection<Appointment> LastAppointments { get; private set; } = [];
        public DateTime LastNow { get; private set; }

        public IReadOnlyCollection<DateTime> GetAvailableSlots(
            Service service,
            IReadOnlyCollection<Appointment> appointments,
            DateOnly date,
            DateTime now)
        {
            GetAvailableSlotsCallCount++;
            LastAppointments = appointments;
            LastNow = now;
            return availableSlots;
        }

        public IReadOnlyCollection<DateOnly> GetAvailableDates(
            Service service,
            IReadOnlyCollection<Appointment> appointments,
            DateOnly from,
            DateOnly to,
            DateTime now)
            => throw new NotImplementedException();
    }

    private sealed class FakeAppointmentRepository(IReadOnlyCollection<Appointment> appointments) : IAppointmentRepository
    {
        public int GetByServiceAndDateCallCount { get; private set; }
        public DateOnly? RequestedDate { get; private set; }

        public Task<IReadOnlyCollection<Appointment>> GetByServiceAndDate(
            int serviceId,
            DateOnly date,
            CancellationToken ct = default)
        {
            GetByServiceAndDateCallCount++;
            RequestedDate = date;
            return Task.FromResult(appointments);
        }

        public Task<IReadOnlyCollection<Appointment>> GetByServiceAndDateRange(int serviceId, DateOnly from, DateOnly to, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<List<Appointment>> GetPendingFutureByServiceAndDateRange(int serviceId, DateOnly from, DateOnly to, DateTime now, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<List<Appointment>> GetExpiredPendingByServices(IReadOnlyCollection<int> serviceIds, DateTime startOfToday, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyCollection<Appointment>> GetByService(int serviceId, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<bool> ExistsByServiceId(int serviceId, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyCollection<Appointment>> SearchByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly? from,
            DateOnly? to,
            AppointmentStatus? status,
            string? clientSearch,
            string? clientEmail,
            bool orderDescending,
            CancellationToken ct = default)
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

    private sealed class StubDateTimeProvider : IDateTimeProvider
    {
        public DateTime NowArgentina() => ReferenceNow;
        public DateTime UtcNow() => ReferenceNow.ToUniversalTime();
    }
}
