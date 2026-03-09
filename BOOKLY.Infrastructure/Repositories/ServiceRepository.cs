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
                .Include(s => s.ServiceSchedulesUnavailability)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public Task<bool> ExistsBlock(int id, DateTime startDateTime, DateTime endDateTime, CancellationToken ct = default)
        {
            var date = DateOnly.FromDateTime(startDateTime);
            var startTime = TimeOnly.FromDateTime(startDateTime);
            var endTime = TimeOnly.FromDateTime(endDateTime);

            return dbContext.Services
                .Where(s => s.Id == id)
                .SelectMany(s => s.ServiceSchedulesUnavailability)
                .AnyAsync(u =>
                    u.Date == date && (
                        u.Range == null ||
                        (u.Range.Start < endTime && u.Range.End > startTime)
                    ), ct);
        }
        public Task<Service?> GetOneWithSchedulesAndUnavailability(int id, CancellationToken ct = default)
        {
            return dbContext.Services
                .AsSplitQuery()
                .Include(s => s.ServiceSchedules)
                .Include(s => s.ServiceSchedulesUnavailability)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
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
    }
}