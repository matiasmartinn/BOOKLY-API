namespace BOOKLY.Domain.Aggregates.UserAggregate
{
    public record Password
    {
        public string Hash { get; }

        private Password(string hash) => Hash = hash;

        /// <summary>
        /// Crea un VO de contraseña ya hashed.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        /// <exception cref="DomainException"></exception>
        public static Password FromHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new DomainException("El hash de contraseña es requerido.");

            return new Password(hash);
        }

        /// <summary>
        /// Validar que cumpla el formato de contraseña.
        /// </summary>
        /// <param name="plainText"></param>
        /// <exception cref="DomainException"></exception>
        public static void AssertPlainTextIsValid(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new DomainException("La contraseña es requerida");

            if(plainText.Length < 8)
                throw new DomainException("La contraseña debe tener al menos 8 caracteres.");

            if(plainText.Length > 128)
                throw new DomainException("La contraseña no puede exceder los 128 caracteres.");

            if(!plainText.Any(char.IsDigit))
                throw new DomainException("La contraseña debe contener al menos un número.");

            if(!plainText.Any(char.IsUpper))
                throw new DomainException("La contraseña debe contener al menos una mayúscula.");
        }
    }
}
