using AutoMapper;
using BOOKLY.Application.Mappings;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Interfaces;

namespace BOOKLY.Application.Services.UserAggregate
{
    public sealed class UserDtoMapper : IUserDtoMapper
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly IMapper _mapper;

        public UserDtoMapper(IServiceRepository serviceRepository, IMapper mapper)
        {
            _serviceRepository = serviceRepository;
            _mapper = mapper;
        }

        public async Task<UserDto> Map(User user, CancellationToken ct = default)
        {
            IReadOnlyCollection<int> serviceIds = [];

            if (user.Role == UserRole.Secretary)
                serviceIds = await _serviceRepository.GetServiceIdsBySecretary(user.Id, ct);

            return _mapper.Map<UserDto>(user, options =>
            {
                options.Items[UserMappingProfile.ServiceIdsContextKey] = serviceIds;
            });
        }
    }
}
