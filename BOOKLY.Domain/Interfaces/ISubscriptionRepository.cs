using BOOKLY.Domain.Aggregates.SubscriptionAggregate;

namespace BOOKLY.Domain.Repositories
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByOwnerId(int ownerId, CancellationToken ct = default);
        Task<Subscription?> GetByOwnerIdForUpdate(int ownerId, CancellationToken ct = default);
        Task AddOne(Subscription subscription, CancellationToken ct = default);
        void Update(Subscription subscription);
    }
}
