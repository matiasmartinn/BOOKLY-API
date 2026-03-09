using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure
{
    public abstract class Repository<T> where T : class
    {
        protected readonly BooklyDbContext dbContext;
        protected DbSet<T> Set => dbContext.Set<T>();
        protected Repository(BooklyDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public virtual Task<T?> GetOne(int id, CancellationToken cancellationToken = default)
        {
            //permite usar find async new object?[] { id }
            return Set.FindAsync(new object?[] { id }, cancellationToken).AsTask();
        }
        public virtual Task AddOne(T entity, CancellationToken cancellationToken = default)
        {
            return Set.AddAsync(entity, cancellationToken).AsTask();
        }
        public virtual void Update(T entity)
        {
            Set.Update(entity);
        }
        public virtual void Remove(T entity)
        {
            Set.Remove(entity);
        }
    }
}
