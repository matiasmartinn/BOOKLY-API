using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.AdminAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IAdminOwnersService
    {
        Task<Result<AdminPagedResultDto<AdminOwnerListItemDto>>> GetOwners(AdminOwnersQueryDto dto, CancellationToken ct = default);
        Task<Result> EnableOwner(int id, CancellationToken ct = default);
        Task<Result> DisableOwner(int id, CancellationToken ct = default);
    }
}
