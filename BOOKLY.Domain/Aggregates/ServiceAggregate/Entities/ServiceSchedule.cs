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
    }
}
