using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Queries;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Repositories
{
    public sealed class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(BooklyDbContext context) : base(context) { }

        public override Task<Appointment?> GetOne(int id, CancellationToken cancellationToken = default)
        {
            return dbContext.Appointments
                .Include(a => a.FieldValues)
                .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsOverlap(
            int serviceId,
            DateTime start,
            DateTime end,
            int? excludedAppointmentId = null,
            CancellationToken ct = default)
        {
            return await dbContext.Appointments
                .Where(a => a.ServiceId == serviceId
                        && (!excludedAppointmentId.HasValue || a.Id != excludedAppointmentId.Value)
                        && a.Status != AppointmentStatus.Cancelled
                        && a.StartDateTime < end
                        && a.EndDateTime > start)
                .AsNoTracking()
                .AnyAsync(ct);
        }

        public async Task<IReadOnlyCollection<Appointment>> GetByService(int serviceId, CancellationToken ct = default)
        {
            return await dbContext.Appointments
                .Include(a => a.FieldValues)
                .Where(a => a.ServiceId == serviceId)
                .OrderByDescending(a => a.StartDateTime)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyCollection<Appointment>> SearchByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly? from,
            DateOnly? to,
            AppointmentStatus? status,
            string? clientSearch,
            string? clientEmail,
            bool orderDescending,
            CancellationToken ct = default)
        {
            if (serviceIds.Count == 0)
                return [];

            var query = dbContext.Appointments
                .Include(a => a.FieldValues)
                .Where(a => serviceIds.Contains(a.ServiceId))
                .AsNoTracking();

            if (from.HasValue)
            {
                var fromDate = from.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(a => a.StartDateTime >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(a => a.StartDateTime < toDate);
            }

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(clientEmail))
            {
                var normalizedEmail = clientEmail.Trim().ToLowerInvariant();
                query = query.Where(a => a.Client.Email.Value == normalizedEmail);
            }

            if (!string.IsNullOrWhiteSpace(clientSearch))
            {
                var term = clientSearch.Trim();
                query = query.Where(a =>
                    a.Client.ClientName.Contains(term) ||
                    a.Client.Email.Value.Contains(term) ||
                    a.Client.Phone.Contains(term));
            }

            query = orderDescending
                ? query.OrderByDescending(a => a.StartDateTime)
                : query.OrderBy(a => a.StartDateTime);

            return await query.ToListAsync(ct);
        }

        public Task<int> CountByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly from,
            DateOnly to,
            int? secretaryId = null,
            CancellationToken ct = default)
        {
            return BuildMetricsQuery(serviceIds, from, to, secretaryId).CountAsync(ct);
        }

        public async Task<IReadOnlyCollection<AppointmentStatusCountResult>> GetStatusCountsByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly from,
            DateOnly to,
            int? secretaryId = null,
            CancellationToken ct = default)
        {
            var rows = await BuildMetricsQuery(serviceIds, from, to, secretaryId)
                .GroupBy(a => a.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    TotalAppointments = group.Count()
                })
                .ToListAsync(ct);

            return rows
                .Select(row => new AppointmentStatusCountResult(row.Status, row.TotalAppointments))
                .ToList();
        }

        public async Task<IReadOnlyCollection<AppointmentDayCountResult>> GetDayCountsByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly from,
            DateOnly to,
            int? secretaryId = null,
            CancellationToken ct = default)
        {
            var rows = await BuildMetricsQuery(serviceIds, from, to, secretaryId)
                .GroupBy(a => a.StartDateTime.Date)
                .Select(group => new
                {
                    Date = group.Key,
                    TotalAppointments = group.Count()
                })
                .OrderBy(row => row.Date)
                .ToListAsync(ct);

            return rows
                .Select(row => new AppointmentDayCountResult(
                    DateOnly.FromDateTime(row.Date),
                    row.TotalAppointments))
                .ToList();
        }

        public async Task<IReadOnlyCollection<AppointmentHourCountResult>> GetHourCountsByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly from,
            DateOnly to,
            int? secretaryId = null,
            CancellationToken ct = default)
        {
            var startDateTimes = await BuildMetricsQuery(serviceIds, from, to, secretaryId)
                .Select(a => a.StartDateTime)
                .ToListAsync(ct);

            return startDateTimes
                .GroupBy(dateTime => dateTime.Hour)
                .OrderBy(group => group.Key)
                .Select(group => new AppointmentHourCountResult(group.Key, group.Count()))
                .ToList();
        }

        public async Task<IReadOnlyCollection<AppointmentWeekdayCountResult>> GetWeekdayCountsByServices(
            IReadOnlyCollection<int> serviceIds,
            DateOnly from,
            DateOnly to,
            int? secretaryId = null,
            CancellationToken ct = default)
        {
            var startDateTimes = await BuildMetricsQuery(serviceIds, from, to, secretaryId)
                .Select(a => a.StartDateTime)
                .ToListAsync(ct);

            return startDateTimes
                .GroupBy(dateTime => (int)dateTime.DayOfWeek)
                .OrderBy(group => NormalizeWeekdayOrder(group.Key))
                .Select(group => new AppointmentWeekdayCountResult(group.Key, group.Count()))
                .ToList();
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

        public async Task<IReadOnlyCollection<Appointment>> GetByServiceAndDateRange(int serviceId, DateOnly from, DateOnly to, CancellationToken ct = default)
        {
            var start = from.ToDateTime(TimeOnly.MinValue);
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

            return await dbContext.Appointments
                .Where(a => a.ServiceId == serviceId
                         && a.StartDateTime >= start
                         && a.StartDateTime < end
                         && a.Status != AppointmentStatus.Cancelled)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Appointment>> GetPendingFutureByServiceAndDateRangeForUpdate(
            int serviceId,
            DateOnly from,
            DateOnly to,
            DateTime now,
            CancellationToken ct = default)
        {
            var start = from.ToDateTime(TimeOnly.MinValue);
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

            return await dbContext.Appointments
                .Where(a => a.ServiceId == serviceId
                         && a.Status == AppointmentStatus.Pending
                         && a.StartDateTime > now
                         && a.EndDateTime > start
                         && a.StartDateTime < end)
                .OrderBy(a => a.StartDateTime)
                .ToListAsync(ct);
        }

        private IQueryable<Appointment> BuildMetricsQuery(
            IReadOnlyCollection<int> serviceIds,
            DateOnly from,
            DateOnly to,
            int? secretaryId)
        {
            if (serviceIds.Count == 0)
            {
                return dbContext.Appointments
                    .Where(_ => false)
                    .AsNoTracking();
            }

            var start = from.ToDateTime(TimeOnly.MinValue);
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var query = dbContext.Appointments
                .Where(a => serviceIds.Contains(a.ServiceId)
                         && a.StartDateTime >= start
                         && a.StartDateTime < end)
                .AsNoTracking();

            if (secretaryId.HasValue)
                query = query.Where(a => a.AssignedSecretaryId == secretaryId.Value);

            return query;
        }

        private static int NormalizeWeekdayOrder(int dayOfWeek)
            => dayOfWeek == (int)DayOfWeek.Sunday ? 7 : dayOfWeek;
    }
}
