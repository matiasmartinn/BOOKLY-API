using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Events;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;

public class RecordStatusChangedHandler : IDomainEventHandler<AppointmentStatusChangedEvent>
{
    private readonly IAppointmentHistoryRepository _historyRepository;
    public RecordStatusChangedHandler(IAppointmentHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public async Task Handle(AppointmentStatusChangedEvent @event, CancellationToken ct)
        => await _historyRepository.AddOne(
            AppointmentStatusHistory.Create(
                @event.AppointmentId,
                @event.OldStatus,
                @event.NewStatus,
                @event.Reason,
                @event.OccurredOn), ct);
}