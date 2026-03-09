namespace BOOKLY.Domain.Aggregates.UserAggregate
{
    public sealed class UserInvitation
    {
        public int Id { get; private set; }
        public int UserId { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public DateTime ExpiresOn { get; private set; }
        public DateTime CreatedOn { get; private set; }
        public DateTime? UsedOn { get; private set; }
        private UserInvitation() { }
        public static UserInvitation Create(int userId, string tokenHash, DateTime now, TimeSpan ttl)
        {
            if (userId <= 0) throw new DomainException("UserId inválido.");
            if (string.IsNullOrWhiteSpace(tokenHash)) throw new DomainException("TokenHash requerido.");
            if (ttl <= TimeSpan.Zero) throw new DomainException("TTL inválido.");

            return new UserInvitation
            {
                UserId = userId,
                TokenHash = tokenHash,
                CreatedOn = now,
                ExpiresOn = now.Add(ttl)
            };
        }

        public bool IsExpired(DateTime now) => now >= ExpiresOn;
        public bool IsUsed => UsedOn.HasValue;

        public void MarkAsUsed(DateTime now)
        {
            if (IsUsed) throw new DomainException("La invitación ya fue utilizada.");
            if (IsExpired(now)) throw new DomainException("La invitación está vencida.");

            UsedOn = now;
        }
    }
}
