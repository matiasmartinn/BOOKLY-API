using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Events;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Application.EventHandler
{
    public class RecordAppointmentCreatedHandler : IDomainEventHandler<AppointmentCreatedEvent>
    {
        private readonly IAppointmentHistoryRepository _historyRepository;

        public RecordAppointmentCreatedHandler(IAppointmentHistoryRepository appointmentHistoryRepository)
        {
            _historyRepository = appointmentHistoryRepository;
        }

        public async Task Handle(AppointmentCreatedEvent @event, CancellationToken ct)
            => await _historyRepository.AddOne(
                AppointmentStatusHistory.Create(
                    @event.AppointmentId,
                    null,
                    AppointmentStatus.Pending,
                    null,
                    @event.OccurredOn,
                    @event.UserId),
                ct);
    }
}
