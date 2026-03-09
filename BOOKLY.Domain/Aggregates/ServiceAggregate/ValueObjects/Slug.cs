namespace BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects
{
    public record Slug
    {
        public string Value { get; init; } = null!;
        private Slug(string value)
        {
            Value = value;
        }
        public static Slug Create(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new DomainException("El slug es requerido.");

            var normalized = text
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "-");

            return new Slug(normalized);
        }
        public override string ToString() => Value;
    }



}
