namespace BOOKLY.Application.Services.SubscriptionAggregate.Dto
{
    public sealed record SubscriptionDto
    {
        public int Id { get; init; }
        public int OwnerId { get; init; }
        public int Status { get; init; } // o SubscriptionStatus si lo preferís en DTO
        public int PlanName { get; init; } // PlanName enum como int
        public int MaxServices { get; init; }
        public int MaxSecretaries { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public DateTime CreatedOn { get; init; }
        public DateTime? UpdateOn { get; init; }
    }
}
