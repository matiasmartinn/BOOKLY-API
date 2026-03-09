// BOOKLY.Infrastructure/Persistence/Configurations/SubscriptionRepository.cs
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Repositories
{
    public sealed class SubscriptionRepository : Repository<Subscription>, ISubscriptionRepository
    {
        public SubscriptionRepository(BooklyDbContext context) : base(context) { }
        public Task<Subscription?> GetByOwnerId(int ownerId, CancellationToken ct = default)
        {
            return dbContext.Set<Subscription>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.OwnerId == ownerId, ct);
        }

        public Task<Subscription?> GetByOwnerIdForUpdate(int ownerId, CancellationToken ct = default)
        {
            return dbContext.Set<Subscription>()
                // Tracking por default
                .FirstOrDefaultAsync(s => s.OwnerId == ownerId, ct);
        }
    }
}
