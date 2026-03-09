using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Repositories
{
    public sealed class UserInvitationRepository : Repository<UserInvitation>, IUserInvitationRepository
    {
        public UserInvitationRepository(BooklyDbContext context) :base(context) { }
        public async Task<UserInvitation?> GetByTokenHash(string tokenHash, CancellationToken ct = default)
        {
            return await dbContext.Set<UserInvitation>()
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
        }
    }
}
