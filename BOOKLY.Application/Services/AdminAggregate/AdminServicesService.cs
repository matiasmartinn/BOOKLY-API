using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Mappings;
using BOOKLY.Application.Services.AdminAggregate.DTOs;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Queries;

namespace BOOKLY.Application.Services.AdminAggregate
{
    public sealed class AdminServicesService : IAdminServicesService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMapper _mapper;

        public AdminServicesService(
            IAdminRepository adminRepository,
            IServiceRepository serviceRepository,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IMapper mapper)
        {
            _adminRepository = adminRepository;
            _serviceRepository = serviceRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
            _mapper = mapper;
        }

        public async Task<Result<AdminPagedResultDto<AdminServiceListItemDto>>> GetServices(AdminServicesQueryDto dto, CancellationToken ct = default)
        {
            if (dto.Page <= 0)
            {
                return Result<AdminPagedResultDto<AdminServiceListItemDto>>.Failure(
                    Error.Validation("Page debe ser mayor a 0."));
            }

            if (dto.PageSize <= 0 || dto.PageSize > 100)
            {
                return Result<AdminPagedResultDto<AdminServiceListItemDto>>.Failure(
                    Error.Validation("PageSize debe estar entre 1 y 100."));
            }

            if (dto.OwnerId.HasValue && dto.OwnerId.Value <= 0)
            {
                return Result<AdminPagedResultDto<AdminServiceListItemDto>>.Failure(
                    Error.Validation("OwnerId invalido."));
            }

            if (!AdminContractsSupport.TryNormalizeServiceStatus(dto.Status, out var normalizedStatus))
            {
                return Result<AdminPagedResultDto<AdminServiceListItemDto>>.Failure(
                    Error.Validation("El estado indicado no es valido."));
            }

            if (!AdminContractsSupport.TryParsePlan(dto.Plan, out var planFilter))
            {
                return Result<AdminPagedResultDto<AdminServiceListItemDto>>.Failure(
                    Error.Validation("El plan indicado no es valido."));
            }

            var today = DateOnly.FromDateTime(_dateTimeProvider.NowArgentina());
            var query = new AdminServiceListQuery(
                dto.Search,
                normalizedStatus,
                dto.OwnerId,
                dto.OwnerSearch,
                planFilter,
                dto.Page,
                dto.PageSize);

            var (items, totalCount) = await _adminRepository.GetServices(query, today, ct);
            var mappedItems = _mapper.Map<List<AdminServiceListItemDto>>(items, options =>
            {
                options.Items[AdminMappingProfile.TodayContextKey] = today;
            });

            return Result<AdminPagedResultDto<AdminServiceListItemDto>>.Success(
                new AdminPagedResultDto<AdminServiceListItemDto>
                {
                    Items = mappedItems,
                    Page = dto.Page,
                    PageSize = dto.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)dto.PageSize)
                });
        }

        public async Task<Result> EnableService(int id, CancellationToken ct = default)
        {
            if (id <= 0)
            {
                return Result.Failure(Error.Validation("Id invalido."));
            }

            var service = await _serviceRepository.GetOne(id, ct);
            if (service == null)
            {
                return Result.Failure(Error.NotFound("Servicio"));
            }

            service.Activate();
            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result> DisableService(int id, CancellationToken ct = default)
        {
            if (id <= 0)
            {
                return Result.Failure(Error.Validation("Id invalido."));
            }

            var service = await _serviceRepository.GetOne(id, ct);
            if (service == null)
            {
                return Result.Failure(Error.NotFound("Servicio"));
            }

            service.Deactivate();
            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }
    }
}
