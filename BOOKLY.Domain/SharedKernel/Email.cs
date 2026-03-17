using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.SharedKernel
{
    public record Email
    {
        public string Value { get; }

        private Email(string value)
        {
            Value = value.ToLowerInvariant();
        }
        public static Email Create(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("El email es obligatorio.");

            if (!email.Contains("@") || !email.Contains("."))
                throw new DomainException("Formato de email inválido.");

            return new Email(email.Trim());
        }
        public override string ToString() => Value;
    }
}
