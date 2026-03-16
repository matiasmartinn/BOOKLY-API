using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetOne(int id, CancellationToken ct = default);
        Task<User?> GetByEmail(string email, CancellationToken ct = default);
        Task<bool> ExistsByEmail(string email, CancellationToken ct = default);
        Task AddOne(User user, CancellationToken ct = default);
        void Update(User user);
        void Remove(User user);
    }
}
