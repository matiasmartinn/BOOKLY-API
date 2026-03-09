namespace BOOKLY.Application.Services.SubscriptionAggregate.Dto
{
    public sealed record RenewSubscriptionDto
    {
        public int OwnerId { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
    }
}
