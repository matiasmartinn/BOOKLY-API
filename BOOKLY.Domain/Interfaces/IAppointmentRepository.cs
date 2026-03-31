using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Queries;

namespace BOOKLY.Domain.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<IReadOnlyCollection<Appointment>> GetByServiceAndDate(int serviceId, DateOnly date, CancellationToken ct = default);
        Task<IReadOnlyCollection<Appointment>> GetByServiceAndDateRange(int serviceId, DateOnly from, DateOnly to, CancellationToken ct = default);
        Task<IReadOnlyCollection<Appointment>> GetByService(int serviceId, CancellationToken ct = default);
        Task<IReadOnlyCollection<Appointment>> SearchByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly? from,
            DateOnly? to,
            AppointmentStatus? status,
            string? clientSearch,
            string? clientEmail,
            bool orderDescending,
            CancellationToken ct = default);
        Task<int> CountByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly from,
            DateOnly to,
            int? secretaryId = null,
            CancellationToken ct = default);
        Task<IReadOnlyCollection<AppointmentStatusCountResult>> GetStatusCountsByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly from,
            DateOnly to,
            int? secretaryId = null,
            CancellationToken ct = default);
        Task<IReadOnlyCollection<AppointmentDayCountResult>> GetDayCountsByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly from,
            DateOnly to,
            int? secretaryId = null,
            CancellationToken ct = default);
        Task<IReadOnlyCollection<AppointmentHourCountResult>> GetHourCountsByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly from,
            DateOnly to,
            int? secretaryId = null,
            CancellationToken ct = default);
        Task<IReadOnlyCollection<AppointmentWeekdayCountResult>> GetWeekdayCountsByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly from,
            DateOnly to,
            int? secretaryId = null,
            CancellationToken ct = default);
        Task<bool> ExistsOverlap(
            int serviceId,
            DateTime start,
            DateTime end,
            int? excludedAppointmentId = null,
            CancellationToken ct = default);
        Task<Appointment?> GetOne(int id, CancellationToken ct = default);
        Task AddOne(Appointment appointment, CancellationToken ct = default);
        void Update(Appointment appointment);
        void Remove(Appointment appointment);
    }
}
