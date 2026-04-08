using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Repositories
{
    public sealed class AppointmentHistoryRepository : Repository<AppointmentStatusHistory>, IAppointmentHistoryRepository
    {
        public AppointmentHistoryRepository(BooklyDbContext context) : base(context) { }

        public async Task<List<AppointmentStatusHistory>> GetByAppointment(int appointmentId, CancellationToken ct = default)
        {
            return await dbContext.AppointmentStatusHistories
                .Include(h => h.User)
                .Where(h => h.AppointmentId == appointmentId)
                .OrderBy(h => h.OccurredOn)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<AppointmentStatusHistory>> GetByService(int serviceId, CancellationToken ct = default)
        {
            return await dbContext.AppointmentStatusHistories
                .Include(h => h.User)
                .Where(h => dbContext.Appointments
                    .Where(a => a.ServiceId == serviceId)
                    .Select(a => a.Id)
                    .Contains(h.AppointmentId))
                .OrderBy(h => h.OccurredOn)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
