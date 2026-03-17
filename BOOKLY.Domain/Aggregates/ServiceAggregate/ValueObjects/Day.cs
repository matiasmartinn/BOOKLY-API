using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects
{
    public record Day
    {
        public int Value { get; init; }
        private Day(int value) => Value = value;
        public static Day Create(int value)
        {
            if (value < 0 || value > 6)
                throw new DomainException("Rango invalido: (Entre 0 y 6)");
            return new Day(value);
        }
    }
}
