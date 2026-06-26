using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;

namespace BOOKLY.Application.Services.SubscriptionAggregate;


public sealed class EffectiveSubscriptionResolver : IEffectiveSubscriptionResolver
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EffectiveSubscriptionResolver(ISubscriptionRepository subscriptionRepository, IDateTimeProvider dateTimeProvider)
    {
        _subscriptionRepository = subscriptionRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Subscription> Resolve(int ownerId, CancellationToken ct = default)
    {
        var subscription = await _subscriptionRepository.GetByOwnerId(ownerId, ct);
        var now = _dateTimeProvider.NowArgentina();
        var today = DateOnly.FromDateTime(now);

        if (subscription == null || !subscription.IsActive(today))
            return Subscription.CreateFree(ownerId, now);

        return subscription;
    }
}
