using BOOKLY.Domain.Aggregates.UserAggregate.Enums;

namespace BOOKLY.Application.Services.UserAggregate
{
    public sealed record IssuedUserToken(string RawToken, TimeSpan Ttl)
    {
        public int TtlHours => (int)Ttl.TotalHours;
    }

    public interface IUserTokenIssuer
    {
        // Registra el token en el repositorio pero no persiste: el caso de uso decide cuándo llamar a SaveChanges.
        Task<IssuedUserToken> CreateToken(int userId, UserTokenPurpose purpose, CancellationToken ct = default);
    }
}
