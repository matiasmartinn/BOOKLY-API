using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.ServiceTypeAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IServiceTypeService
    {
        Task<Result<ServiceTypeDto?>> GetById(int id, CancellationToken ct = default);
        Task<Result<ServiceTypeDto?>> GetByIdWithFields(int id, CancellationToken ct = default);
        Task<Result<List<ServiceTypeDto>>> GetAll(CancellationToken ct = default);
        Task<Result<ServiceTypeDto>> CreateServiceType(CreateServiceTypeDto dto, CancellationToken ct = default);
        Task<Result<ServiceTypeDto>> UpdateServiceType(int id, UpdateServiceTypeDto dto, CancellationToken ct = default);
        Task<Result> DeleteServiceType(int id, CancellationToken ct = default);

        // Fields
        Task<Result<ServiceTypeDto>> AddField(int serviceTypeId, AddServiceTypeFieldDto dto, CancellationToken ct = default);
        Task<Result<ServiceTypeDto>> UpdateField(int serviceTypeId, int fieldId, UpdateServiceTypeFieldDto dto, CancellationToken ct = default);
        Task<Result> RemoveField(int serviceTypeId, int fieldId, CancellationToken ct = default);
        Task<Result> ActivateField(int serviceTypeId, int fieldId, CancellationToken ct = default);

        // Options
        Task<Result<ServiceTypeDto>> AddOption(int serviceTypeId, int fieldId, AddServiceTypeFieldOptionDto dto, CancellationToken ct = default);
        Task<Result<ServiceTypeDto>> UpdateOption(int serviceTypeId, int fieldId, int optionId, UpdateServiceTypeFieldOptionDto dto, CancellationToken ct = default);
        Task<Result> RemoveOption(int serviceTypeId, int fieldId, int optionId, CancellationToken ct = default);
        Task<Result> DeactivateOption(int serviceTypeId, int fieldId, int optionId, CancellationToken ct = default);
        Task<Result> ActivateOption(int serviceTypeId, int fieldId, int optionId, CancellationToken ct = default);
    }
}
