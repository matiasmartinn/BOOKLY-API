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
            var normalizedEmail = NormalizeEmail(email);
            return Set.AnyAsync(user => user.Email.Value == normalizedEmail, ct);
        }

        public async Task<User?> GetByEmail(string email, CancellationToken ct = default)
        {
            var normalizedEmail = NormalizeEmail(email);
            return await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, ct);
        }

        private static string NormalizeEmail(string email)
            => email.Trim().ToLowerInvariant();
    }
}
