using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(BooklyDbContext context) : base(context) { }

        public Task<User?> GetById(int id, CancellationToken ct = default)
        {
            return dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, ct);
        }

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

        public Task<RefreshToken?> GetRefreshToken(string token, CancellationToken ct = default)
        {
            return dbContext.RefreshTokens.FirstOrDefaultAsync(refreshToken => refreshToken.Token == token, ct);
        }

        public Task AddRefreshToken(RefreshToken refreshToken, CancellationToken ct = default)
        {
            return dbContext.RefreshTokens.AddAsync(refreshToken, ct).AsTask();
        }

        public async Task RevokeAllUserTokens(int userId, CancellationToken ct = default)
        {
            var tokens = await dbContext.RefreshTokens
                .Where(refreshToken => refreshToken.UserId == userId && !refreshToken.IsRevoked)
                .ToListAsync(ct);

            foreach (var refreshToken in tokens)
            {
                refreshToken.Revoke();
            }
        }

        private static string NormalizeEmail(string email)
            => email.Trim().ToLowerInvariant();
    }
}
