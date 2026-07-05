using BOOKLY.Application.Common.Models;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Domain.Interfaces;

namespace BOOKLY.Application.Services.SlotValidationService;

public interface ISlotValidationService
{
    Task<Result> ValidateSlotAvailability(
        Service service,
        DateTime requestedStart,
        int? excludedAppointmentId,
        bool requireActiveService,
        CancellationToken ct = default);
}

public sealed class SlotValidationService : ISlotValidationService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IAvailabilityService _availabilityService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SlotValidationService(IAppointmentRepository appointmentRepository, IAvailabilityService availabilityService, IDateTimeProvider dateTimeProvider)
    {
        _appointmentRepository = appointmentRepository;
        _availabilityService = availabilityService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> ValidateSlotAvailability(
        Service service,
        DateTime requestedStart,
        int? excludedAppointmentId,
        bool requireActiveService,
        CancellationToken ct = default)
    {
        var now = _dateTimeProvider.NowArgentina();
        if (requireActiveService && !service.IsActive)
            return Result.Failure(Error.Conflict("El servicio no se encuentra activo."));

        if (requestedStart <= now)
            return Result.Failure(Error.Validation("El turno debe agendarse en un horario futuro."));

        if (service.GetScheduleFor(requestedStart, service.DurationMinutes) is null)
            return Result.Failure(Error.Validation("El horario no pertenece a la agenda del servicio."));

        var date = DateOnly.FromDateTime(requestedStart);
        var appointments = await _appointmentRepository.GetByServiceAndDate(service.Id, date, ct);
        var appointmentsToEvaluate = excludedAppointmentId.HasValue
            ? appointments.Where(a => a.Id != excludedAppointmentId.Value).ToList()
            : appointments;

        var availableSlots = _availabilityService.GetAvailableSlots(service, appointmentsToEvaluate, date, now);

        if (!availableSlots.Contains(requestedStart))
            return Result.Failure(Error.Conflict("El horario seleccionado ya no está disponible."));

        return Result.Success();
    }
}
