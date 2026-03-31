using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.MetricsAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IMetricsService
    {
        Task<Result<AppointmentMetricsDto>> GetAppointmentMetrics(AppointmentMetricsQueryDto dto, CancellationToken ct = default);
    }
}
