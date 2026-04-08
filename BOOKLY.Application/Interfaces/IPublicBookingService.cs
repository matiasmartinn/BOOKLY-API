using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Application.Services.PublicBooking.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface IPublicBookingService
    {
        Task<Result<PublicServiceBookingDto>> GetService(string slug, string token, CancellationToken ct = default);
        Task<Result<List<DateOnly>>> GetAvailableDates(string slug, string token, DateOnly? from, DateOnly? to, CancellationToken ct = default);
        Task<Result<List<DateTime>>> GetAvailableSlots(string slug, string token, DateOnly date, CancellationToken ct = default);
        Task<Result<AppointmentDto>> CreateAppointment(string slug, string token, PublicCreateAppointmentDto dto, CancellationToken ct = default);
    }
}
