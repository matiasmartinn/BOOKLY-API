using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Persistence.Repositories
{
    public class ServiceRepository : Repository<Service>, IServiceRepository
    {
        public ServiceRepository(BooklyDbContext context) : base(context) { }

        public Task<Service?> GetOneWithSchedules(int id, CancellationToken ct = default)
        {
            return dbContext.Services
                .AsSplitQuery()
                .Include(s => s.ServiceSchedules)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }
        public Task<Service?> GetOneWithUnavailability(int id, CancellationToken ct = default)
        {
            return dbContext.Services
                .AsSplitQuery()
                .Include(s => s.ServicesUnavailability)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public Task<bool> ExistsBlock(int id, DateTime startDateTime, DateTime endDateTime, CancellationToken ct = default)
        {
            var date = DateOnly.FromDateTime(startDateTime);
            var startTime = TimeOnly.FromDateTime(startDateTime);
            var endTime = TimeOnly.FromDateTime(endDateTime);

            return dbContext.Services
                .Where(s => s.Id == id)
                .SelectMany(s => s.ServicesUnavailability)
                .AnyAsync(u =>
                    u.DateRange.Start <= date && 
                    u.DateRange.End >= date &&
                    (
                        u.TimeRange == null || 
                        (u.TimeRange.Start < endTime && u.TimeRange.End > startTime)
                    ), ct);
        }
        public Task<Service?> GetOneWithSchedulesAndUnavailability(int id, CancellationToken ct = default)
        {
            return dbContext.Services
                .AsSplitQuery()
                .Include(s => s.ServiceSchedules)
                .Include(s => s.ServicesUnavailability)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task<IEnumerable<ServiceSchedule?>> GetSchedulesByService(int serviceId, CancellationToken ct = default)
        {
            return await dbContext.Services
                .Where(s => s.Id == serviceId)
                .SelectMany(s => s.ServiceSchedules)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<ServiceUnavailability?>> GetUnavailabilityByService(int serviceId, CancellationToken ct = default)
        {
            return await dbContext.Services
                .Where(s => s.Id == serviceId)
                .SelectMany(s => s.ServicesUnavailability)
                .AsNoTracking()
                .ToListAsync(ct);
        }
        public async Task<int> CountByOwnerId(int ownerId, CancellationToken ct = default)
        {
            return await dbContext.Services
                .AsNoTracking()
                .Where(s => s.OwnerId == ownerId)
                .CountAsync(ct);
        }

        public async Task<int> CountAssignedSecretariesByOwnerId(int ownerId, CancellationToken ct = default)
        {
            return await (
                from ss in dbContext.Set<ServiceSecretary>().AsNoTracking()
                join s in dbContext.Set<Service>().AsNoTracking()
                    on ss.ServiceId equals s.Id
                where s.OwnerId == ownerId
                select ss.SecretaryId
            ).Distinct().CountAsync(ct);
        }

        public Task<List<Service>> GetServicesByOwner(int ownerId, CancellationToken ct = default)
        {
            return dbContext.Services
                .Where(s => s.OwnerId == ownerId)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}