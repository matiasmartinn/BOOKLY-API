using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.AdminAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IAdminServicesService
    {
        Task<Result<AdminPagedResultDto<AdminServiceListItemDto>>> GetServices(AdminServicesQueryDto dto, CancellationToken ct = default);
        Task<Result> EnableService(int id, CancellationToken ct = default);
        Task<Result> DisableService(int id, CancellationToken ct = default);
    }
}
