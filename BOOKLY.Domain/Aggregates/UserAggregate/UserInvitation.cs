using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Aggregates.UserAggregate
{
    public sealed class UserToken
    {
        public int Id { get; private set; }
        public int UserId { get; private set; }
        public UserTokenPurpose Purpose { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public DateTime ExpiresOn { get; private set; }
        public DateTime CreatedOn { get; private set; }
        public DateTime? UsedOn { get; private set; }
        private UserToken() { }
        public static UserToken Create(int userId, UserTokenPurpose purpose, string tokenHash, DateTime now, TimeSpan ttl)
        {
            if (userId <= 0) throw new DomainException("UserId inválido.");
            if (string.IsNullOrWhiteSpace(tokenHash)) throw new DomainException("TokenHash requerido.");
            if (ttl <= TimeSpan.Zero) throw new DomainException("TTL inválido.");

            return new UserToken
            {
                UserId = userId,
                Purpose = purpose,
                TokenHash = tokenHash,
                CreatedOn = now,
                ExpiresOn = now.Add(ttl)
            };
        }

        public bool IsExpired(DateTime now) => now >= ExpiresOn;
        public bool IsUsed => UsedOn.HasValue;

        public void MarkAsUsed(DateTime now)
        {
            if (IsUsed) throw new DomainException("El token ya fue utilizado.");
            if (IsExpired(now)) throw new DomainException("El token está vencido.");

            UsedOn = now;
        }
    }
}
