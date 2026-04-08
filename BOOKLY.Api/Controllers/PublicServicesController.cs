using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Application.Services.PublicBooking.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Route("api/public/services")]
    public sealed class PublicServicesController : BaseController
    {
        private readonly IPublicBookingService _publicBookingService;

        public PublicServicesController(IPublicBookingService publicBookingService)
        {
            _publicBookingService = publicBookingService;
        }

        [HttpGet("{slug}/{token}")]
        [ProducesResponseType(typeof(PublicServiceBookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GetService(string slug, string token, CancellationToken ct)
        {
            return HandleResult(await _publicBookingService.GetService(slug, token, ct));
        }

        [HttpGet("{slug}/{token}/available-dates")]
        [ProducesResponseType(typeof(List<DateOnly>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GetAvailableDates(
            string slug,
            string token,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            CancellationToken ct)
        {
            return HandleResult(await _publicBookingService.GetAvailableDates(slug, token, from, to, ct));
        }

        [HttpGet("{slug}/{token}/available-slots")]
        [ProducesResponseType(typeof(List<DateTime>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GetAvailableSlots(
            string slug,
            string token,
            [FromQuery] DateOnly date,
            CancellationToken ct)
        {
            return HandleResult(await _publicBookingService.GetAvailableSlots(slug, token, date, ct));
        }

        [HttpPost("{slug}/{token}/appointments")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAppointment(
            string slug,
            string token,
            [FromBody] PublicCreateAppointmentDto dto,
            CancellationToken ct)
        {
            var result = await _publicBookingService.CreateAppointment(slug, token, dto, ct);
            return result.IsSuccess
                ? StatusCode(StatusCodes.Status201Created, result.Data)
                : HandleResult(result);
        }
    }
}
