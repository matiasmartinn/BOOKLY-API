using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;

namespace BOOKLY.Application.Interfaces
{
    public interface IAppointmentCancellationNotificationService
    {
        Task NotifyAppointmentCancelled(
            Service service,
            Appointment appointment,
            bool notifyOwner,
            CancellationToken ct = default);
    }
}
