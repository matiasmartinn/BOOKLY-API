using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;

namespace BOOKLY.Domain.Aggregates.ServiceAggregate.Entities
{
    public class ServiceSchedule
    {
        public int Id { get; private set; }
        public TimeRange Range { get; private set; } = null!;
        public Capacity Capacity { get; private set; } = null!;
        public Day Day { get; private set; } = null!;

        private ServiceSchedule() { }

        public static ServiceSchedule Create(
            TimeRange range,
            Capacity capacity,
            Day day
            )
        {
            return new ServiceSchedule
            {
                Range = range,
                Capacity = capacity,
                Day = day
            };
        }

        public bool CanHost(DateTime startDateTime, Duration duration)
        {
            if (Day.Value != (int)startDateTime.DayOfWeek)
                return false;

            var start = TimeOnly.FromDateTime(startDateTime);
            var end = TimeOnly.FromDateTime(startDateTime.AddMinutes(duration.Value));

            return Range.Contains(start, end);
        }
    }
}
