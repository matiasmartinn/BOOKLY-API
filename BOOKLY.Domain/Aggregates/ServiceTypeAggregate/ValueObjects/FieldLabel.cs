// BOOKLY.Infrastructure/Persistence/Configurations/SubscriptionRepository.cs
using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Aggregates.ServiceTypeAggregate.ValueObjects
{
    public sealed record FieldLabel
    {
        public string Value { get; init; } = null!;

        private FieldLabel(string value) => Value = value;

        public static FieldLabel Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("El label del campo es requerido.");

            var trimmed = value.Trim();

            if (trimmed.Length < 2)
                throw new DomainException("El label debe tener al menos 2 caracteres.");

            if (trimmed.Length > 40)
                throw new DomainException("El label no puede exceder 40 caracteres.");

            return new FieldLabel(trimmed);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
