using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.Interfaces;

namespace BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects
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

        public bool Verify(string plaintText, IPasswordHasher hasher) => hasher.Verify(plaintText, Hash);
    }
}
