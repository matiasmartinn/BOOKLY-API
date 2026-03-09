using BOOKLY.Domain.Aggregates.UserAggregate;

namespace BOOKLY.Domain.Interfaces
{
    public interface IUserInvitationRepository
    {
        Task AddOne(UserInvitation userInvitation, CancellationToken ct = default);
        Task<UserInvitation?> GetByTokenHash(string tokenHash, CancellationToken ct = default);
        void Update(UserInvitation invitation);
    }
}
