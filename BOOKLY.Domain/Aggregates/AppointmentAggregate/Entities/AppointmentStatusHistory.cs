using BOOKLY.Domain.Aggregates.UserAggregate;

namespace BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities
{
    public sealed class AppointmentStatusHistory
    {
        public int Id { get; private set; }
        public int AppointmentId { get; private set; }
        public int? UserId { get; private set; }
        public AppointmentStatus? OldStatus { get; private set; }
        public AppointmentStatus NewStatus { get; private set; }
        public string? Reason { get; private set; }
        public DateTime OccurredOn { get; private set; }
        public User? User { get; private set; }

        private AppointmentStatusHistory() { }

        public static AppointmentStatusHistory Create(
            int appointmentId,
            AppointmentStatus? oldStatus,
            AppointmentStatus newStatus,
            string? reason,
            DateTime occurredOn,
            int? userId = null) => new()
            {
                AppointmentId = appointmentId,
                UserId = userId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Reason = reason,
                OccurredOn = occurredOn
            };
    }
}
