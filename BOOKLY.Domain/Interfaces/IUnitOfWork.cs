namespace BOOKLY.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChanges(CancellationToken cancellationToken = default);

        // Ejecuta la operación dentro de una única transacción de base de datos:
        // los SaveChanges intermedios participan de ella y nada se commitea hasta el final.
        Task<T> ExecuteInTransaction<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    }
}
