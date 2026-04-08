using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Mappings;
using BOOKLY.Application.Services.AdminAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Queries;

namespace BOOKLY.Application.Services.AdminAggregate
{
    public sealed class AdminOwnersService : IAdminOwnersService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMapper _mapper;

        public AdminOwnersService(
            IAdminRepository adminRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IMapper mapper)
        {
            _adminRepository = adminRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
            _mapper = mapper;
        }

        public async Task<Result<AdminPagedResultDto<AdminOwnerListItemDto>>> GetOwners(AdminOwnersQueryDto dto, CancellationToken ct = default)
        {
            if (dto.Page <= 0)
            {
                return Result<AdminPagedResultDto<AdminOwnerListItemDto>>.Failure(
                    Error.Validation("Page debe ser mayor a 0."));
            }

            if (dto.PageSize <= 0 || dto.PageSize > 100)
            {
                return Result<AdminPagedResultDto<AdminOwnerListItemDto>>.Failure(
                    Error.Validation("PageSize debe estar entre 1 y 100."));
            }

            if (dto.CreatedFrom.HasValue && dto.CreatedTo.HasValue && dto.CreatedFrom.Value > dto.CreatedTo.Value)
            {
                return Result<AdminPagedResultDto<AdminOwnerListItemDto>>.Failure(
                    Error.Validation("El rango CreatedFrom/CreatedTo es invalido."));
            }

            if (!AdminContractsSupport.TryNormalizeOwnerStatus(dto.Status, out var normalizedStatus))
            {
                return Result<AdminPagedResultDto<AdminOwnerListItemDto>>.Failure(
                    Error.Validation("El estado indicado no es valido."));
            }

            if (!AdminContractsSupport.TryParsePlan(dto.Plan, out var planFilter))
            {
                return Result<AdminPagedResultDto<AdminOwnerListItemDto>>.Failure(
                    Error.Validation("El plan indicado no es valido."));
            }

            var today = DateOnly.FromDateTime(_dateTimeProvider.NowArgentina());
            var query = new AdminOwnerListQuery(
                dto.Search,
                normalizedStatus,
                planFilter,
                dto.CreatedFrom,
                dto.CreatedTo,
                dto.Page,
                dto.PageSize);

            var (items, totalCount) = await _adminRepository.GetOwners(query, today, ct);
            var mappedItems = _mapper.Map<List<AdminOwnerListItemDto>>(items, options =>
            {
                options.Items[AdminMappingProfile.TodayContextKey] = today;
            });

            return Result<AdminPagedResultDto<AdminOwnerListItemDto>>.Success(
                new AdminPagedResultDto<AdminOwnerListItemDto>
                {
                    Items = mappedItems,
                    Page = dto.Page,
                    PageSize = dto.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)dto.PageSize)
                });
        }

        public async Task<Result> EnableOwner(int id, CancellationToken ct = default)
        {
            if (id <= 0)
            {
                return Result.Failure(Error.Validation("Id invalido."));
            }

            var owner = await _userRepository.GetOne(id, ct);
            if (owner == null || owner.Role != UserRole.Owner)
            {
                return Result.Failure(Error.NotFound("Owner"));
            }

            owner.Activate();
            _userRepository.Update(owner);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result> DisableOwner(int id, CancellationToken ct = default)
        {
            if (id <= 0)
            {
                return Result.Failure(Error.Validation("Id invalido."));
            }

            var owner = await _userRepository.GetOne(id, ct);
            if (owner == null || owner.Role != UserRole.Owner)
            {
                return Result.Failure(Error.NotFound("Owner"));
            }

            owner.Deactivate();
            _userRepository.Update(owner);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }
    }
}
