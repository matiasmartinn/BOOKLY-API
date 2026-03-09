using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Aggregates.AppointmentAggregate.Events
{
    public record AppointmentCreatedEvent(
        int AppointmentId,
        int ServiceId,
        string ClientName,
        string ClientEmail,
        DateTime StartDateTime,
        DateTime OccurredOn
        ) : IDomainEvent;
}
