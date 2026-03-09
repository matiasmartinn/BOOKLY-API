namespace BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects
{
    /// <summary>
    /// Horario de inicio y fin.
    /// </summary>
    public record TimeRange
    {
        public TimeOnly Start { get; init; }
        public TimeOnly End { get; init; }
        private TimeRange() { }
        private TimeRange(TimeOnly start, TimeOnly end)
        {
            Start = start;
            End = end;
        }
        public static TimeRange Create(TimeOnly start, TimeOnly end)
        {
            if (end < start)
                throw new DomainException("El tiempo de inicio debe ser menor que el de fin");
            return new TimeRange(start, end);
        }
        public bool OverlapsWith(TimeRange other)
        {
            return Start < other.End && End > other.Start;
        }
    }
}
