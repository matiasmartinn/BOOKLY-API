using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Aggregates.AppointmentAggregate.Events
{
    public record AppointmentStatusChangedEvent(
        int AppointmentId,
        AppointmentStatus OldStatus,
        AppointmentStatus NewStatus,
        string? Reason,
        DateTime OccurredOn,
        int? UserId
        ) : IDomainEvent;
}
