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
            DateOnly date);
    }
    public class AvailabilityService : IAvailabilityService
    {
        public IReadOnlyCollection<DateTime> GetAvailableSlots(
            Service service,
            IReadOnlyCollection<Appointment> appointments,
            DateOnly date)
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
            var now = DateTime.Now;

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

                    var hasOverlap = appointments.Any(a =>
                        a.StartDateTime < slotEnd &&
                        a.EndDateTime > slotStart);

                    if (!hasOverlap)
                        result.Add(slotStart);
                }
            }

            return result.AsReadOnly();
        }
    }
}
