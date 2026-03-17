using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : BaseController
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }
        #region Queries

        /// <summary>
        /// Obtiene todos los turnos asociados a un servicio específico.
        /// </summary>
        [HttpGet("{serviceId}")]
        [ProducesResponseType(typeof(List<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByService(int serviceId, CancellationToken ct)
        {
            return HandleResult(await _appointmentService.GetByService(serviceId, ct));
        }
        /// <summary>
        /// Obtiene los turnos de un servicio específico filtrados por una fecha determinada.
        /// </summary>
        [HttpGet("{serviceId}/date/{date}")]
        [ProducesResponseType(typeof(List<AppointmentSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByServiceAndDate(int serviceId, DateOnly date, CancellationToken ct)
        {
            return HandleResult(await _appointmentService.GetByServiceAndDate(serviceId, date, ct));
        }
        #endregion

        #region Commands
        /// <summary>
        /// Crea un nuevo turno para un servicio, validando disponibilidad y reglas de negocio.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto, CancellationToken ct)
        {
            var result = await _appointmentService.CreateAppointment(dto, ct);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetByService), new { serviceId = result.Data?.ServiceId }, result.Data)
                : HandleResult(result);
        }

        #endregion

        #region Estado
        /// <summary>
        /// Confirma un turno pendiente.
        /// </summary>
        [HttpPatch("{id}/confirm")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Confirm(int id, CancellationToken ct)
        {
            return HandleResult(await _appointmentService.Confirm(id, ct));
        }

        /// <summary>
        /// Cancela un turno existente indicando el motivo de cancelación.
        /// </summary>
        [HttpPatch("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelAppointmentDto dto, CancellationToken ct)
        {
            return HandleResult(await _appointmentService.Cancel(id, dto, ct));
        }
        /// <summary>
        /// Marca un turno como completado una vez finalizado el servicio.
        /// </summary>
        [HttpPatch("{id}/complete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAsCompleted(int id, CancellationToken ct)
        {
            return HandleResult(await _appointmentService.MarkAsCompleted(id, ct));
        }
        /// <summary>
        /// Marca un turno como "no asistido" cuando el cliente no se presenta.
        /// </summary>
        [HttpPatch("{id}/no-show")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAsNoShow(int id, CancellationToken ct)
        {
            return HandleResult(await _appointmentService.MarkAsNoShow(id, ct));
        }

        #endregion

    }
}
