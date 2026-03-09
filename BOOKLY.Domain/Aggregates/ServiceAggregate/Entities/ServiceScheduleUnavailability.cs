using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;

namespace BOOKLY.Domain.Aggregates.ServiceAggregate.Entities
{
    public sealed class ServiceScheduleUnavailability
    {
        public int Id { get; private set; }
        public DateOnly Date { get; private set; }
        public TimeRange? Range { get; private set; }
        public string? Reason { get; private set; }

        private ServiceScheduleUnavailability() { }
        public static ServiceScheduleUnavailability Create(DateOnly date, TimeRange? range, string? reason)
        {
            if (date < DateOnly.FromDateTime(DateTime.Today))
                throw new DomainException("No se puede crear una excepción en una fecha pasada.");

            return new ServiceScheduleUnavailability
            {
                Date = date,
                Range = range,
                Reason = reason
            };
        }
    }
}
