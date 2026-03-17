using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Aggregates.UserAggregate
{
    public record PersonName
    {
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public string FullName => $"{FirstName} {LastName}";

        private PersonName(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public static PersonName Create(string firstName, string lastName)
        {
            ValidateAndNormalizeName(firstName, "Nombre");
            ValidateAndNormalizeName(lastName, "Apellido");
            return new PersonName(firstName, lastName);
        }

        private static string ValidateAndNormalizeName(string name, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException($"El {fieldName} es requerido");

            var normalized = name.Trim();

            if (normalized.Length < 2)
                throw new DomainException($"El {fieldName} debe tener al menos 2 caracteres");

            if (normalized.Length > 100)
                throw new DomainException($"El {fieldName} no puede exceder 100 caracteres");

            return normalized;
        }
    }
}
