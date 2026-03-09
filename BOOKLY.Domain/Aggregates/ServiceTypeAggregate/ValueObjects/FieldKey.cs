namespace BOOKLY.Domain.Aggregates.ServiceTypeAggregate.ValueObjects
{
    /// <summary>
    /// Codigo que se almacena en DB.
    /// No se modifica para mantener persistencia
    /// </summary>
    public sealed record FieldKey
    {
        public string Value { get; init; } = null!;
        private FieldKey(string value) => Value = value;

        public static FieldKey Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("La key del campo es requerida.");

            var trimmed = value.Trim();

            if (trimmed.Length < 2)
                throw new DomainException("La key debe tener al menos 2 caracteres.");

            if (trimmed.Length > 60)
                throw new DomainException("La key no puede exceder 60 caracteres.");

            // Formato: a-z, 0-9, _
            foreach (var ch in trimmed)
            {
                var ok = ch >= 'a' && ch <= 'z' ||
                         ch >= '0' && ch <= '9' ||
                         ch == '_';

                if (!ok)
                    throw new DomainException("La key solo puede contener letras minúsculas, números y guión bajo (a-z, 0-9, _).");
            }

            return new FieldKey(trimmed);
        }

        public override string ToString() => Value;
    }
}
