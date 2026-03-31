using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Application.Services.ClientAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IClientService
    {
        Task<Result<IReadOnlyCollection<ClientListItemDto>>> GetByOwner(int ownerId, string? search = null, CancellationToken ct = default);
        Task<Result<ClientDetailDto>> GetDetail(int ownerId, string email, CancellationToken ct = default);
        Task<Result<IReadOnlyCollection<AppointmentListItemDto>>> GetAppointmentHistory(int ownerId, string email, CancellationToken ct = default);
    }
}
