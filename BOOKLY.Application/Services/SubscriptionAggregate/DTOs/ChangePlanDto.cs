using BOOKLY.Domain.Aggregates.SubscriptionAggregate;

namespace BOOKLY.Application.Services.SubscriptionAggregate.Dto
{
    public sealed record ChangePlanDto
    {
        public int OwnerId { get; init; }
        public PlanName PlanName { get; init; }

    }
}
