using BOOKLY.Application.Common.Models;
using BOOKLY.Domain.Aggregates.ServiceAggregate;

namespace BOOKLY.Application.Interfaces
{
    public interface IOwnerServiceScopeResolver
    {
        Task<Result<List<Service>>> Resolve(int? ownerId, int? serviceId, CancellationToken ct = default);
    }
}
