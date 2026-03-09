namespace BOOKLY.Domain.SharedKernel
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}
