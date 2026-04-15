using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Application.Services.PublicBooking.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IPublicBookingService
    {
        Task<Result<PublicServiceBookingDto>> GetService(string slug, string code, CancellationToken ct = default);
        Task<Result<List<DateOnly>>> GetAvailableDates(string slug, string code, DateOnly? from, DateOnly? to, CancellationToken ct = default);
        Task<Result<List<DateTime>>> GetAvailableSlots(string slug, string code, DateOnly date, CancellationToken ct = default);
        Task<Result<AppointmentDto>> CreateAppointment(string slug, string code, PublicCreateAppointmentDto dto, CancellationToken ct = default);
    }
}
