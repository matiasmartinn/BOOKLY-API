namespace BOOKLY.Domain.SharedKernel
{
    public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
    {
        Task Handle(TEvent @event, CancellationToken ct = default);
    }
}
