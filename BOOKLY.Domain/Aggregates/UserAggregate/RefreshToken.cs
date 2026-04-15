using System.Security.Cryptography;
using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Aggregates.UserAggregate
{
    public sealed class RefreshToken
    {
        private static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(7);

        public int Id { get; private set; }
        public int UserId { get; private set; }
        public string Token { get; private set; } = null!;
        public DateTime ExpiresAt { get; private set; }
        public bool IsRevoked { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private RefreshToken()
        {
        }

        public static string GenerateToken()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        public static RefreshToken Create(int userId, string token, DateTime now)
        {
            if (userId <= 0)
                throw new DomainException("UserId invalido.");

            if (string.IsNullOrWhiteSpace(token))
                throw new DomainException("El token es requerido.");

            if (now == default)
                throw new DomainException("La fecha actual es requerida.");

            return new RefreshToken
            {
                UserId = userId,
                Token = token,
                CreatedAt = now,
                ExpiresAt = now.Add(DefaultLifetime),
                IsRevoked = false
            };
        }

        public bool IsValid(DateTime now)
            => !IsRevoked && now < ExpiresAt;

        public void Revoke()
        {
            if (IsRevoked)
                return;

            IsRevoked = true;
        }
    }
}
