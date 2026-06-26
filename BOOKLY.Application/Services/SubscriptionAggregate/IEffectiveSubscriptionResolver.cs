using BOOKLY.Domain.Aggregates.SubscriptionAggregate;

namespace BOOKLY.Application.Services.SubscriptionAggregate;

public interface IEffectiveSubscriptionResolver
{
    Task<Subscription> Resolve(int ownerId, CancellationToken ct = default);
}