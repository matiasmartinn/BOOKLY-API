using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;

namespace BOOKLY.Domain.Aggregates.ServiceAggregate.Entities
{
    public sealed class ServiceUnavailability
    {
        public int Id { get; private set; }
        public DateRange DateRange { get; private set; } = default!;
        public TimeRange? TimeRange { get; private set; }
        public string? Reason { get; private set; }

        private ServiceUnavailability() { }

        private ServiceUnavailability(DateRange dateRange, TimeRange? timeRange, string? reason)
        {
            DateRange = dateRange;
            TimeRange = timeRange;
            Reason = reason;
        }

        public static ServiceUnavailability Create(
            DateRange dateRange,
            TimeRange? timeRange,
            string? reason)
        {
            return new ServiceUnavailability(dateRange, timeRange, reason);
        }

        public bool AppliesTo(DateOnly date)
        {
            return DateRange.Contains(date);
        }

        public bool BlocksWholeDay(DateOnly date)
        {
            return AppliesTo(date) && TimeRange is null;
        }

        public bool BlocksRange(DateOnly date, TimeRange range)
        {
            if (!AppliesTo(date))
                return false;

            if (TimeRange is null)
                return true;

            return TimeRange.OverlapsWith(range);
        }

        public bool OverlapsWith(ServiceUnavailability other)
        {
            if (!DateRange.OverlapsWith(other.DateRange))
                return false;

            if (TimeRange is null || other.TimeRange is null)
                return true;

            return TimeRange.OverlapsWith(other.TimeRange);
        }
    }
}