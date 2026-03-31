using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(BooklyDbContext context) : base(context) { }

        public Task<bool> ExistsByEmail(string email, CancellationToken ct = default)
        {
            return Set.AnyAsync(user => user.Email.Value == email, ct);
        }

        public async Task<User?> GetByEmail(string email, CancellationToken ct = default)
        {
            return await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email.Value == email, ct);
        }
    }
}
