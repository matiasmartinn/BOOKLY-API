using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Aggregates.SubscriptionAggregate
{
    public sealed record SubscriptionPeriod
    {
        public DateOnly StartDate { get; private set; }
        public DateOnly? EndDate { get; private set; }
        public bool IsOpenEnded => EndDate == null;

        // EF Core ctor
        private SubscriptionPeriod() { }
        
        private SubscriptionPeriod(DateOnly start, DateOnly? end)
        {
            StartDate = start;
            EndDate = end;
        }

        public static SubscriptionPeriod Create(DateOnly start, DateOnly endInclusive)
        {
            if (endInclusive < start)
                throw new DomainException("El período es inválido (End > start)");

            return new SubscriptionPeriod(start, endInclusive);
        }

        public static SubscriptionPeriod OpenEnded(DateOnly start)
        {
            return new SubscriptionPeriod(start, null);
        }
    }
}
