using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;

namespace BOOKLY.Domain.Aggregates.ServiceAggregate.Services;

public static class UnavailabilityAppointmentsAffected
{
    public static bool IsAppointmentAffectedByUnavailability(
        Appointment appointment,
        ServiceUnavailability unavailability)
    {
        var appointmentDate = DateOnly.FromDateTime(appointment.StartDateTime);
        var appointmentRange = TimeRange.Create(
            TimeOnly.FromDateTime(appointment.StartDateTime),
            TimeOnly.FromDateTime(appointment.EndDateTime));

        return unavailability.BlocksRange(appointmentDate, appointmentRange);
    }
}