using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.AdminAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<Result<AdminDashboardDto>> GetDashboard(AdminDashboardQueryDto dto, CancellationToken ct = default);
    }
}
