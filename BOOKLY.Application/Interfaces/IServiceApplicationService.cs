using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.ServiceAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IServiceApplicationService
    {
        Task<Result<ServiceDto>> GetServiceById(int id, CancellationToken ct = default);
        Task<Result<List<ServiceDto>>> GetServicesByOwner(int ownerId, CancellationToken ct = default);
        Task<Result<ServiceDto>> CreateService(CreateServiceDto dto, CancellationToken ct = default);
        Task<Result<ServiceDto>> UpdateService(int id, UpdateServiceDto dto, CancellationToken ct = default);
        Task<Result> DeleteService(int id, CancellationToken ct = default);
        Task<Result<List<ServiceScheduleDto>>> GetSchedulesByService(int serviceId, CancellationToken ct = default);
        Task<Result<List<ScheduleUnavailabilityDto>>> GetUnavailabilityByService(int serviceId, CancellationToken ct = default);
        Task<Result<ServiceDto>> SetSecretaries(int id, SetSecretariesDto dto, CancellationToken ct = default);
        Task<Result<ServiceDto>> SetSchedule(int id, List<CreateServiceScheduleDto> dto, CancellationToken ct = default);
        Task<Result<List<ScheduleUnavailabilityDto>>> GetScheduleUnavailability(int id, CancellationToken ct= default);
        Task<Result> AddUnavailability(int id, CreateUnavailabilityDto dto, CancellationToken ct = default);
        Task<Result> RemoveUnavailability(int id, int unavailabilityId, CancellationToken ct = default);
        Task<Result> Activate(int id, CancellationToken ct = default);
        Task<Result> Deactivate(int id, CancellationToken ct = default);
        Task<Result<List<DateTime>>> GetAvailableSlots(int id, DateOnly date, CancellationToken ct = default);
        Task<Result<List<DateOnly>>> GetAvailableDates(int id, DateOnly from, DateOnly to, CancellationToken ct = default);
    }
}
