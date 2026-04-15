using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;

namespace BOOKLY.Domain.Interfaces
{
    public interface IServiceRepository
    {
        Task<Service?> GetOne(int id, CancellationToken ct = default);
        Task<Service?> GetBySlug(string slug, CancellationToken ct = default);
        Task<Service?> GetBySlugAndPublicBookingCode(string slug, string publicBookingCode, CancellationToken ct = default);
        Task<Service?> GetOneWithSchedules(int id, CancellationToken ct = default);
        Task<Service?> GetOneWithUnavailability(int id, CancellationToken ct = default);
        Task<Service?> GetOneWithSecretaries(int id, CancellationToken ct = default);
        Task<Service?> GetOneWithSchedulesAndUnavailability(int id, CancellationToken ct = default);
        Task<Service?> GetBySlugWithSchedulesAndUnavailability(string slug, CancellationToken ct = default);
        Task<Service?> GetBySlugAndPublicBookingCodeWithSchedulesAndUnavailability(string slug, string publicBookingCode, CancellationToken ct = default);
        Task<List<ServiceSchedule>> GetSchedulesByService(int serviceId, CancellationToken ct = default);
        Task<List<ServiceUnavailability>> GetUnavailabilityByService(int serviceId, CancellationToken ct = default);
        Task<List<Service>> GetServicesByOwner(int ownerId, CancellationToken ct = default);
        Task<List<Service>> GetServicesByOwnerWithSecretaries(int ownerId, CancellationToken ct = default);
        Task<List<int>> GetServiceIdsBySecretary(int secretaryId, CancellationToken ct = default);
        Task<List<int>> GetOwnerIdsBySecretary(int secretaryId, CancellationToken ct = default);
        Task<bool> ExistsSlug(string slug, int? excludedServiceId = null, CancellationToken ct = default);
        Task<bool> ExistsPublicBookingCode(string publicBookingCode, int? excludedServiceId = null, CancellationToken ct = default);
        Task<bool> ExistsBlock(int id, DateTime startDateTime, DateTime endDateTime, CancellationToken ct = default);
        Task<int> CountByOwnerId(int ownerId, CancellationToken ct = default);
        Task<int> CountActiveByOwnerId(int ownerId, CancellationToken ct = default);
        Task<int> CountAssignedSecretariesByOwnerId(int ownerId, CancellationToken ct = default);
        Task AddOne(Service service, CancellationToken ct = default);
        void Update(Service service);
        void Remove(Service service);
    }
}
