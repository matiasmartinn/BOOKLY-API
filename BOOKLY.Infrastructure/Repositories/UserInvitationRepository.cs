using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Repositories
{
    public sealed class UserInvitationRepository : Repository<UserToken>, IUserTokenRepository
    {
        public UserInvitationRepository(BooklyDbContext context) : base(context)
        {
        }

        public async Task<UserToken?> GetByTokenHash(string tokenHash, CancellationToken ct = default)
        {
            return await dbContext.Set<UserToken>()
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
        }
    }
}
