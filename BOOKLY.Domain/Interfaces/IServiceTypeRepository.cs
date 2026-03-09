using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;

namespace BOOKLY.Domain.Interfaces
{
    public interface IServiceTypeRepository
    {
        Task<ICollection<ServiceType>> GetAll(CancellationToken ct = default);
        Task<ServiceType?> GetByIdWithFields(int id, CancellationToken ct = default);
        Task<ServiceType?> GetOne(int id, CancellationToken ct = default);
        Task AddOne(ServiceType serviceType, CancellationToken ct = default);
        void Update(ServiceType serviceType);
        void Remove(ServiceType serviceType);
    }
}
