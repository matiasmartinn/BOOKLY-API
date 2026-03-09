using BOOKLY.Domain.Aggregates.AppointmentAggregate;

namespace BOOKLY.Domain.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<IReadOnlyCollection<Appointment>> GetByServiceAndDate(int serviceId, DateOnly date, CancellationToken ct = default);
        Task<IReadOnlyCollection<Appointment>> GetByService(int serviceId, CancellationToken ct = default);
        Task<bool> ExistsOverlap(int serviceId, DateTime start, DateTime end, CancellationToken ct = default);
        Task<Appointment?> GetOne(int id, CancellationToken ct = default);
        Task AddOne(Appointment appointment, CancellationToken ct = default);
        void Update(Appointment appointment);
        void Remove(Appointment appointment);
    }
}
