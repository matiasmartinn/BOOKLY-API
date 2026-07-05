using BOOKLY.Domain.Aggregates.SubscriptionAggregate;

namespace BOOKLY.Domain.Repositories
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByOwnerId(int ownerId, CancellationToken ct = default);
        // Devuelve la suscripción trackeada por EF para poder modificarla; no toma locks de base de datos.
        Task<Subscription?> GetByOwnerIdTracked(int ownerId, CancellationToken ct = default);
        Task AddOne(Subscription subscription, CancellationToken ct = default);
        void Update(Subscription subscription);
    }
}
