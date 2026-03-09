using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(BooklyDbContext context) : base(context) { }

        public Task<bool> ExistsByEmail(Email email, CancellationToken ct = default)
        {
            return Set.AnyAsync(user => user.Email.Value == email.Value, ct);
        }
    }
}
