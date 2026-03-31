using BOOKLY.Domain.Aggregates.UserAggregate;

namespace BOOKLY.Domain.Interfaces
{
    public interface IUserTokenRepository
    {
        Task AddOne(UserToken userToken, CancellationToken ct = default);
        Task<UserToken?> GetByTokenHash(string tokenHash, CancellationToken ct = default);
        void Update(UserToken token);
    }
}
