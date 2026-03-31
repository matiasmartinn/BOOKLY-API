using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Aggregates.AppointmentAggregate.Events
{
    public sealed record AppointmentCreatedEvent(
        Appointment Appointment,
        DateTime OccurredOn,
        int? UserId
        ) : IDomainEvent
    {
        public int AppointmentId => Appointment.Id;
        public int ServiceId => Appointment.ServiceId;
        public string ClientName => Appointment.Client.ClientName;
        public string ClientEmail => Appointment.Client.Email.Value;
        public DateTime StartDateTime => Appointment.StartDateTime;
    }
}
