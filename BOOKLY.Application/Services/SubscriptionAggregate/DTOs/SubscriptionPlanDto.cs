namespace BOOKLY.Application.Services.SubscriptionAggregate.Dto
{
    public sealed record SubscriptionPlanDto
    {
        public int Code { get; init; }
        public string Key { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public SubscriptionPlanLimitsDto Limits { get; init; } = null!;
    }
}
