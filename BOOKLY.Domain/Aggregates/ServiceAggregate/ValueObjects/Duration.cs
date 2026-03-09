namespace BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects
{
    public record Duration
    {
        public int Value { get; init; }
        private Duration(int value) => Value = value;
        public static Duration Create(int minutes)
        {
            if (minutes <= 0)
                throw new DomainException("La duración debe ser mayor a 0.");
            if (minutes > 24 * 60)
                throw new DomainException("La duración no puede exceder 24 horas.");
            return new Duration(minutes);
        }
    }
}
