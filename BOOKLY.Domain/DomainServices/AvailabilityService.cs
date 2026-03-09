using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;

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
            // 1. Día completamente bloqueado
            var isFullDayBlocked = service.ServiceSchedulesUnavailability
                .Any(u => u.Date == date && u.Range == null);

            if (isFullDayBlocked)
                return [];

            // 2. Schedules del día de la semana
            var schedulesForDay = service.ServiceSchedules
                .Where(s => s.Day.Value == (int)date.DayOfWeek)
                .OrderBy(s => s.Range.Start)
                .ToList();

            if (schedulesForDay.Count == 0)
                return [];

            // 3. Rangos parcialmente bloqueados del día
            var blockedRanges = service.ServiceSchedulesUnavailability
                .Where(u => u.Date == date && u.Range != null)
                .Select(u => u.Range!)
                .ToList();

            var slotDuration = TimeSpan.FromMinutes(service.DurationMinutes.Value);
            var result = new List<DateTime>();

            foreach (var schedule in schedulesForDay)
            {
                var windowStart = date.ToDateTime(schedule.Range.Start);
                var windowEnd = date.ToDateTime(schedule.Range.End);
                var dateTimeNow = DateTime.Now;

                for (var slotStart = windowStart; slotStart + slotDuration <= windowEnd; slotStart += slotDuration)
                {
                    var slotEnd = slotStart + slotDuration;

                    var isBlocked = blockedRanges.Any(r =>
                        slotStart < date.ToDateTime(r.End) &&
                        slotEnd > date.ToDateTime(r.Start));

                    if (isBlocked)
                        continue;

                    if (dateTimeNow > slotStart)
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
