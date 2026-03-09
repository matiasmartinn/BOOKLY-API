using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Interfaces
{
    public interface IDomainEventDispatcher
    {
        Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
    }
}
