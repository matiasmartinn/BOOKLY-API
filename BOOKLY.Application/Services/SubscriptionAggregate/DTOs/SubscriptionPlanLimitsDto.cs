namespace BOOKLY.Application.Services.SubscriptionAggregate.Dto
{
    public sealed record SubscriptionPlanLimitsDto
    {
        public int MaxServices { get; init; }
        public int MaxSecretaries { get; init; }
        public bool AllowsExtraFields { get; init; }
    }
}
