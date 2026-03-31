using AutoMapper;
using BOOKLY.Application.Common;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.ServiceTypeAggregate.DTOs;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Enum;
using BOOKLY.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BOOKLY.Application.Services.ServiceTypeAggregate
{
    public sealed class ServiceTypeService : IServiceTypeService
    {
        private readonly IServiceTypeRepository _serviceTypeRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public ServiceTypeService(
            IServiceTypeRepository serviceTypeRepository,
            IDateTimeProvider dateTimeProvider,
            IMapper mapper,
            IUnitOfWork unitOfWork
            )
        {
            _serviceTypeRepository = serviceTypeRepository;
            _dateTimeProvider = dateTimeProvider;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<ServiceTypeDto>>> GetAll(CancellationToken ct = default)
        {
            var serviceTypes = await _serviceTypeRepository.GetAll(ct);
            var dto = _mapper.Map<List<ServiceTypeDto>>(serviceTypes);
            return Result<List<ServiceTypeDto>>.Success(dto);
        }

        public async Task<Result<ServiceTypeDto?>> GetById(int id, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetOne(id, ct);
            if (serviceType == null)
                return Result<ServiceTypeDto?>.Failure(Error.NotFound("TipoServicio"));

            return Result<ServiceTypeDto?>.Success(_mapper.Map<ServiceTypeDto>(serviceType));
        }

        public async Task<Result<ServiceTypeDto?>> GetByIdWithFields(int id, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(id, ct);
            if (serviceType == null)
                return Result<ServiceTypeDto?>.Failure(Error.NotFound("TipoServicio"));

            return Result<ServiceTypeDto?>.Success(_mapper.Map<ServiceTypeDto>(serviceType));
        }

        public async Task<Result<ServiceTypeDto>> CreateServiceType(CreateServiceTypeDto dto, CancellationToken ct = default)
        {
            var serviceType = ServiceType.Create(
                dto.Name,
                dto.Description
            );

            await _serviceTypeRepository.AddOne(serviceType, ct);
            await _unitOfWork.SaveChanges(ct);

            return Result<ServiceTypeDto>.Success(_mapper.Map<ServiceTypeDto>(serviceType));
        }

        public async Task<Result<ServiceTypeDto>> UpdateServiceType(int id, UpdateServiceTypeDto dto, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetOne(id, ct);
            if (serviceType == null)
                return Result<ServiceTypeDto>.Failure(Error.NotFound("TipoServicio"));

            if (dto.Name != null)
                serviceType.ChangeName(dto.Name);

            if (dto.Description != null)
                serviceType.ChangeDescription(dto.Description);

            _serviceTypeRepository.Update(serviceType);
            await _unitOfWork.SaveChanges(ct);

            return Result<ServiceTypeDto>.Success(_mapper.Map<ServiceTypeDto>(serviceType));
        }

        public async Task<Result> DeleteServiceType(int id, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetOne(id, ct);
            if (serviceType == null)
                return Result.Failure(Error.NotFound("TipoServicio"));

            serviceType.Deactivate();
            _serviceTypeRepository.Update(serviceType);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        // ========================
        // Field management
        // ========================

        public async Task<Result<ServiceTypeDto>> AddField(int serviceTypeId, AddServiceTypeFieldDto dto, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(serviceTypeId, ct);
            if (serviceType == null)
                return Result<ServiceTypeDto>.Failure(Error.NotFound("TipoServicio"));

            serviceType.AddField(
                dto.Key,
                dto.Label,
                (ServiceFieldType)dto.FieldType,
                dto.IsRequired,
                dto.SortOrder,
                _dateTimeProvider.UtcNow(),
                dto.Description);

            _serviceTypeRepository.Update(serviceType);
            await _unitOfWork.SaveChanges(ct);

            return Result<ServiceTypeDto>.Success(_mapper.Map<ServiceTypeDto>(serviceType));
        }

        public async Task<Result<ServiceTypeDto>> UpdateField(int serviceTypeId, int fieldId, UpdateServiceTypeFieldDto dto, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(serviceTypeId, ct);
            if (serviceType == null)
                return Result<ServiceTypeDto>.Failure(Error.NotFound("TipoServicio"));

            if (dto.Label != null || dto.Description != null || dto.IsRequired.HasValue || dto.SortOrder.HasValue)
            {
                serviceType.UpdateField(
                    fieldId,
                    dto.Label,
                    dto.Description,
                    dto.IsRequired,
                    dto.SortOrder);
            }

            if (dto.FieldType.HasValue)
                serviceType.ChangeFieldType(fieldId, (ServiceFieldType)dto.FieldType.Value);

            _serviceTypeRepository.Update(serviceType);
            await _unitOfWork.SaveChanges(ct);

            return Result<ServiceTypeDto>.Success(_mapper.Map<ServiceTypeDto>(serviceType));
        }

        public async Task<Result> RemoveField(int serviceTypeId, int fieldId, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(serviceTypeId, ct);
            if (serviceType == null)
                return Result.Failure(Error.NotFound("TipoServicio"));

            serviceType.RemoveField(fieldId);
            _serviceTypeRepository.Update(serviceType);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result> ActivateField(int serviceTypeId, int fieldId, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(serviceTypeId, ct);
            if (serviceType == null)
                return Result.Failure(Error.NotFound("TipoServicio"));

            serviceType.ActivateField(fieldId);
            _serviceTypeRepository.Update(serviceType);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        // ========================
        // Option management
        // ========================

        public async Task<Result<ServiceTypeDto>> AddOption(int serviceTypeId, int fieldId, AddServiceTypeFieldOptionDto dto, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(serviceTypeId, ct);
            if (serviceType == null)
                return Result<ServiceTypeDto>.Failure(Error.NotFound("TipoServicio"));

            serviceType.AddOptionToField(fieldId, dto.Value, dto.Label, dto.SortOrder, _dateTimeProvider.UtcNow());
            _serviceTypeRepository.Update(serviceType);
            await _unitOfWork.SaveChanges(ct);

            return Result<ServiceTypeDto>.Success(_mapper.Map<ServiceTypeDto>(serviceType));
        }

        public async Task<Result<ServiceTypeDto>> UpdateOption(int serviceTypeId, int fieldId, int optionId, UpdateServiceTypeFieldOptionDto dto, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(serviceTypeId, ct);
            if (serviceType == null)
                return Result<ServiceTypeDto>.Failure(Error.NotFound("TipoServicio"));

            serviceType.UpdateOption(fieldId, optionId, dto.Label, dto.SortOrder, _dateTimeProvider.UtcNow());
            _serviceTypeRepository.Update(serviceType);
            await _unitOfWork.SaveChanges(ct);

            return Result<ServiceTypeDto>.Success(_mapper.Map<ServiceTypeDto>(serviceType));
        }

        public async Task<Result> RemoveOption(int serviceTypeId, int fieldId, int optionId, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(serviceTypeId, ct);
            if (serviceType == null)
                return Result.Failure(Error.NotFound("TipoServicio"));

            serviceType.RemoveOptionFromField(fieldId, optionId);
            _serviceTypeRepository.Update(serviceType);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result> DeactivateOption(int serviceTypeId, int fieldId, int optionId, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(serviceTypeId, ct);
            if (serviceType == null)
                return Result.Failure(Error.NotFound("TipoServicio"));

            serviceType.DeactivateOption(fieldId, optionId, _dateTimeProvider.UtcNow());
            _serviceTypeRepository.Update(serviceType);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result> ActivateOption(int serviceTypeId, int fieldId, int optionId, CancellationToken ct = default)
        {
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(serviceTypeId, ct);
            if (serviceType == null)
                return Result.Failure(Error.NotFound("TipoServicio"));

            serviceType.ActivateOption(fieldId, optionId, _dateTimeProvider.UtcNow());
            _serviceTypeRepository.Update(serviceType);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }
    }
}
