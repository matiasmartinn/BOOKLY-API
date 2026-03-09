namespace BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects
{
    public record Capacity
    {
        public int Value { get; init; }
        private Capacity(int value) => Value = value;
        public static Capacity Create(int value)
        {
            if (value < 0)
                throw new DomainException("Capacidad minima = 1");

            return new Capacity(value);
        }
    }
}
