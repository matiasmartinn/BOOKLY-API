using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Repositories
{
    public sealed class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(BooklyDbContext context) : base(context) { }

        public async Task<bool> ExistsOverlap(int serviceId, DateTime start, DateTime end, CancellationToken ct = default)
        {
            return await dbContext.Appointments
                .Where(a => a.ServiceId == serviceId
                        && a.Status != AppointmentStatus.Cancelled
                        && a.StartDateTime < end
                        && a.EndDateTime > start)
                .AsNoTracking()
                .AnyAsync(ct);
        }
        public async Task<IReadOnlyCollection<Appointment>> GetByService(int serviceId, CancellationToken ct = default)
        {
            return await dbContext.Appointments
                .Where(a => a.ServiceId == serviceId)
                .OrderByDescending(a => a.StartDateTime)
                .AsNoTracking()
                .ToListAsync(ct);
        }
        public async Task<IReadOnlyCollection<Appointment>> GetByServiceAndDate(int serviceId, DateOnly date, CancellationToken ct = default)
        {
            var start = date.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);

            return await dbContext.Appointments
                .Where(a => a.ServiceId == serviceId
                         && a.StartDateTime >= start
                         && a.StartDateTime < end
                         && a.Status != AppointmentStatus.Cancelled)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
