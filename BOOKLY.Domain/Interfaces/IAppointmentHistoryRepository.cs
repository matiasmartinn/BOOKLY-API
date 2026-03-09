using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;

namespace BOOKLY.Domain.Interfaces
{
    public interface IAppointmentHistoryRepository
    {
        Task AddOne(AppointmentStatusHistory history, CancellationToken ct = default);
        Task<List<AppointmentStatusHistory>> GetByAppointment(int appointmentId, CancellationToken ct = default);
        Task<List<AppointmentStatusHistory>> GetByService(int serviceId, CancellationToken ct = default);
    }
}
