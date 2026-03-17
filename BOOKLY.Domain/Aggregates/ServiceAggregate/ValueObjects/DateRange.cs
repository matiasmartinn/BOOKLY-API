using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects
{
    public record DateRange
    {
        public DateOnly Start { get; init; }
        public DateOnly End { get; init; }
        private DateRange() { }

        private DateRange(DateOnly startRange, DateOnly endRange)
        {
            Start = startRange;
            End = endRange;
        }

        public static DateRange Create(DateOnly start, DateOnly end)
        {
            if (end < start)
                throw new DomainException("La fecha de inicio debe ser menor o igual a la fecha de fin.");

            if (start < DateOnly.FromDateTime(DateTime.Today))
                throw new DomainException("No se puede crear una inhabilitación en fechas pasadas.");

            return new DateRange(start, end);
        }

        public bool Contains(DateOnly date) => date >= Start && date <= End;

        public bool OverlapsWith(DateRange other)
        {
            return Start <= other.End && End >= other.Start;
        }
    }
}
