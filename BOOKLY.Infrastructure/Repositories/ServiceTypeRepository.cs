using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Repositories
{
    public sealed class ServiceTypeRepository : Repository<ServiceType>, IServiceTypeRepository
    {
        public ServiceTypeRepository(BooklyDbContext context) : base(context) { }

        public async Task<ICollection<ServiceType>> GetAll(CancellationToken ct = default)
        {
            return await dbContext.ServiceTypes
                .Where(st => st.IsActive == true)
                .ToListAsync(ct);
        }

        public async Task<ServiceType?> GetByIdWithFields(int id, CancellationToken ct = default)
        {
            return await dbContext.ServiceTypes
                .Include(st => st.FieldDefinitions)
                .ThenInclude(fd => fd.Options)
                .FirstOrDefaultAsync(st => st.Id == id, ct);
        }
    }
}
