using BOOKLY.Domain.Aggregates.SubscriptionAggregate;

namespace BOOKLY.Application.Services.SubscriptionAggregate.Dto
{
    public sealed record ChangePlanDto
    {
        public int OwnerId { get; init; }
        public string? TargetPlan { get; init; }
        public PlanName? PlanName { get; init; }
        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
    }
}
