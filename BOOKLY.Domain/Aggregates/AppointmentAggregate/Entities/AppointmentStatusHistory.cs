namespace BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities
{
    public sealed class AppointmentStatusHistory
    {
        public int Id { get; private set; }
        public int AppointmentId { get; private set; }
        public AppointmentStatus? OldStatus { get; private set; }
        public AppointmentStatus NewStatus { get; private set; }
        public string? Reason { get; private set; }
        public DateTime OccurredOn { get; private set; }

        private AppointmentStatusHistory() { }

        public static AppointmentStatusHistory Create(
            int appointmentId,
            AppointmentStatus? oldStatus,
            AppointmentStatus newStatus,
            string? reason,
            DateTime occurredOn) => new()
            {
                AppointmentId = appointmentId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Reason = reason,
                OccurredOn = occurredOn
            };
    }
}
