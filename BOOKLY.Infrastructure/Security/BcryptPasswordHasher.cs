using BOOKLY.Domain.Interfaces;

namespace BOOKLY.Infrastructure.Security
{
    public sealed class BcryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string plainTextPassword)
    => BCrypt.Net.BCrypt.HashPassword(plainTextPassword);

        public bool Verify(string plainTextPassword, string passwordHash)
            => BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
    }
}
