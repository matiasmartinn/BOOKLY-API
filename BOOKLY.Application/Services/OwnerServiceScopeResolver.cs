using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Interfaces;

namespace BOOKLY.Application.Services
{
    public sealed class OwnerServiceScopeResolver : IOwnerServiceScopeResolver
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly IUserRepository _userRepository;

        public OwnerServiceScopeResolver(
            IServiceRepository serviceRepository,
            IUserRepository userRepository)
        {
            _serviceRepository = serviceRepository;
            _userRepository = userRepository;
        }

        public async Task<Result<List<Service>>> Resolve(int? ownerId, int? serviceId, CancellationToken ct = default)
        {
            if (serviceId.HasValue)
            {
                var service = await _serviceRepository.GetOne(serviceId.Value, ct);
                if (service == null)
                    return Result<List<Service>>.Failure(Error.NotFound("Servicio"));

                if (ownerId.HasValue && service.OwnerId != ownerId.Value)
                    return Result<List<Service>>.Failure(Error.Validation("El servicio no pertenece al owner indicado."));

                return Result<List<Service>>.Success([service]);
            }

            if (!ownerId.HasValue || ownerId.Value <= 0)
                return Result<List<Service>>.Failure(Error.Validation("Debe indicar ownerId o serviceId."));

            var owner = await _userRepository.GetOne(ownerId.Value, ct);
            if (owner == null || owner.Role != UserRole.Owner)
                return Result<List<Service>>.Failure(Error.NotFound("Usuario"));

            var services = await _serviceRepository.GetServicesByOwner(ownerId.Value, ct);
            return Result<List<Service>>.Success(services);
        }
    }
}
