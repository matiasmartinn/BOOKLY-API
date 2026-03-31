using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;

namespace BOOKLY.Domain.DomainServices
{
    public interface IAvailabilityService
    {
        IReadOnlyCollection<DateTime> GetAvailableSlots(
            Service service,
            IReadOnlyCollection<Appointment> appointments,
            DateOnly date,
            DateTime now);

        IReadOnlyCollection<DateOnly> GetAvailableDates(
            Service service,
            IReadOnlyCollection<Appointment> appointments,
            DateOnly from,
            DateOnly to,
            DateTime now);
    }
    public class AvailabilityService : IAvailabilityService
    {
        public IReadOnlyCollection<DateTime> GetAvailableSlots(
            Service service,
            IReadOnlyCollection<Appointment> appointments,
            DateOnly date,
            DateTime now)
        {
            // 1. Día completamente bloqueado por una inhabilitación
            var isFullDayBlocked = service.IsFullDayBlocked(date);
            if (isFullDayBlocked)
                return [];

            // 2. Horarios configurados para el día de semana
            var schedulesForDay = service.ServiceSchedules
                .Where(s => s.Day.Value == (int)date.DayOfWeek)
                .OrderBy(s => s.Range.Start)
                .ToList();

            if (schedulesForDay.Count == 0)
                return [];

            // 3. Rangos parciales bloqueados que aplican a la fecha
            var blockedRanges = service.GetBlockedRanges(date);

            var slotDuration = TimeSpan.FromMinutes(service.DurationMinutes.Value);
            var result = new List<DateTime>();
            foreach (var schedule in schedulesForDay)
            {
                var windowStart = date.ToDateTime(schedule.Range.Start);
                var windowEnd = date.ToDateTime(schedule.Range.End);

                for (
                    var slotStart = windowStart;
                    slotStart + slotDuration <= windowEnd;
                    slotStart += slotDuration)
                {
                    var slotEnd = slotStart + slotDuration;

                    var slotRange = TimeRange.Create(
                        TimeOnly.FromDateTime(slotStart),
                        TimeOnly.FromDateTime(slotEnd));

                    var isBlocked = blockedRanges.Any(r => r.OverlapsWith(slotRange));

                    if (isBlocked)
                        continue;

                    if (now > slotStart)
                        continue;

                    var overlappingAppointments = appointments.Count(a =>
                        a.StartDateTime < slotEnd &&
                        a.EndDateTime > slotStart);

                    if (overlappingAppointments < schedule.Capacity.Value)
                        result.Add(slotStart);
                }
            }

            return result.AsReadOnly();
        }

        public IReadOnlyCollection<DateOnly> GetAvailableDates(
            Service service,
            IReadOnlyCollection<Appointment> appointments,
            DateOnly from,
            DateOnly to,
            DateTime now)
        {
            var result = new List<DateOnly>();

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var appointmentsForDay = appointments
                    .Where(a => DateOnly.FromDateTime(a.StartDateTime) == date)
                    .ToList();

                var slots = GetAvailableSlots(service, appointmentsForDay, date, now);

                if (slots.Count > 0)
                    result.Add(date);
            }

            return result.AsReadOnly();
        }
    }
}
