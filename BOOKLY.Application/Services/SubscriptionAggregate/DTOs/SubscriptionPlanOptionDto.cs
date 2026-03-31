namespace BOOKLY.Application.Services.SubscriptionAggregate.Dto
{
    public sealed record SubscriptionPlanOptionDto
    {
        public SubscriptionPlanDto Plan { get; init; } = null!;
        public bool IsCurrent { get; init; }
        public string ChangeType { get; init; } = string.Empty;
        public bool CanChange { get; init; }
        public bool RequiresPeriod { get; init; }
        public string? UnavailableReason { get; init; }
    }
}
