using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<Result<AppointmentDto>> GetById(int id, CancellationToken ct = default);
        Task<Result<IReadOnlyCollection<AppointmentDto>>> GetByService(int id, CancellationToken ct = default);
        Task<Result<IReadOnlyCollection<AppointmentSummaryDto>>> GetByServiceAndDate(int id, DateOnly date, CancellationToken ct = default);
        Task<Result<IReadOnlyCollection<AppointmentListItemDto>>> GetByDay(AppointmentDayQueryDto dto, CancellationToken ct = default);
        Task<Result<IReadOnlyCollection<AppointmentListItemDto>>> SearchAppointments(AppointmentQueryDto dto, CancellationToken ct = default);
        Task<Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>> GetHistoryByAppointment(int id, CancellationToken ct = default);
        Task<Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>> GetHistoryByService(int serviceId, CancellationToken ct = default);
        Task<Result<AppointmentDto>> CreateAppointment(CreateAppointmentDto dto, CancellationToken ct = default);
        Task<Result<AppointmentDto>> UpdateAppointmentInformation(int id, UpdateAppointmentDto dto, CancellationToken ct = default);
        Task<Result<AppointmentDto>> RescheduleAppointment(int id, RescheduleAppointmentDto dto, CancellationToken ct = default);
        Task<Result> MarkAsCancel(int id, CancelAppointmentDto dto, CancellationToken ct = default);
        Task<Result> MarkAsAttended(int id, int? userId = null, CancellationToken ct = default);
        Task<Result> MarkAsNoShow(int id, int? userId = null, CancellationToken ct = default);
    }
}
