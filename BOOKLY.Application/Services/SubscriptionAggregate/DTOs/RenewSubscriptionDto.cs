namespace BOOKLY.Application.Services.SubscriptionAggregate.Dto
{
    public sealed record RenewSubscriptionDto
    {
        public int OwnerId { get; init; }
    }
}
