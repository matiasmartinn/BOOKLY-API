namespace BOOKLY.Application.Services.SubscriptionAggregate.Dto
{
    public sealed record SubscriptionDto
    {
        public int Id { get; init; }
        public int OwnerId { get; init; }
        public bool IsPersisted { get; init; }
        public SubscriptionPlanDto CurrentPlan { get; init; } = null!;
        public string Status { get; init; } = string.Empty;
        public string RawStatus { get; init; } = string.Empty;
        public int RawStatusCode { get; init; }
        public bool IsActive { get; init; }
        public bool IsCancelled { get; init; }
        public bool IsExpired { get; init; }
        public bool PendingCancellation { get; init; }
        public bool CanCancel { get; init; }
        public bool CanRenew { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public bool IsOpenEnded { get; init; }
        public DateTime? CreatedOn { get; init; }
        public DateTime? UpdatedOn { get; init; }
    }
}
