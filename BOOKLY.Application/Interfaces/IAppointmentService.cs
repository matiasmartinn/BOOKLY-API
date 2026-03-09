using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<Result<List<AppointmentDto>>> GetByService(int id, CancellationToken ct = default);
        Task<Result<List<AppointmentSummaryDto>>> GetByServiceAndDate(int id, DateOnly date, CancellationToken ct = default);
        Task<Result<AppointmentDto>> CreateAppointment(CreateAppointmentDto dto, CancellationToken ct = default);
        Task<Result> Confirm(int id, CancellationToken ct = default);
        Task<Result> Cancel(int id, CancelAppointmentDto dto, CancellationToken ct = default);
        Task<Result> MarkAsCompleted(int id, CancellationToken ct = default);
        Task<Result> MarkAsNoShow(int id, CancellationToken ct = default);
    }
}
