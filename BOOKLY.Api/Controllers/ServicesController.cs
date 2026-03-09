using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AppointmentAggregate;
using BOOKLY.Application.Services.ServiceAggregate.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : BaseController
    {
        private readonly IServiceApplicationService _serviceApplicationService;

        public ServicesController(IServiceApplicationService serviceApplicationService)
        {
            _serviceApplicationService = serviceApplicationService;
        }

        #region Basic Crud
        /// <summary>
        /// Recupera un servicio junto con su configuración actual desde la capa de aplicación.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.GetServiceById(id, ct));
        }
        /// <summary>
        /// Crea un nuevo servicio aplicando validaciones de dominio y persistiendo el agregado.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateServiceDto dto, CancellationToken ct)
        {
            var result = await _serviceApplicationService.CreateService(dto, ct);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result.Data)
                : HandleResult(result);
        }
        /// <summary>
        /// Actualiza los datos principales de un servicio existente aplicando reglas de negocio.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceDto dto, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.UpdateService(id, dto, ct));
        }
        /// <summary>
        /// Elimina lógicamente un servicio del sistema.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct) 
        {
           return HandleResult(await _serviceApplicationService.DeleteService(id, ct));
        }
        #endregion

        #region Schedules
        /// <summary>
        /// Define o reemplaza la configuración de horarios disponibles de un servicio.
        /// </summary>
        [HttpPut("{id}/schedules")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetSchedules(
           int id,
           [FromBody] List<CreateServiceScheduleDto> dto,
           CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.SetSchedule(id, dto, ct));
        }
        /// <summary>
        /// Calcula y devuelve los turnos disponibles para un servicio en una fecha determinada.
        /// </summary>
        [HttpGet("service/{serviceId}/available-slots/{date}")]
        [ProducesResponseType(typeof(List<DateTime>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAvailableSlots(int serviceId, DateOnly date, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.GetAvailableSlots(serviceId, date, ct));
        }

        #endregion

        #region UNAVAILABILITIES
        /// <summary>
        /// Registra una excepción de disponibilidad para un servicio en una fecha específica.
        /// </summary>
        [HttpPost("{id}/unavailability")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddUnavailability(
            int id,
            [FromBody] CreateUnavailabilityDto dto,
            CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.AddUnavailability(id, dto, ct));
        }
        /// <summary>
        /// Elimina una excepción de disponibilidad previamente configurada.
        /// </summary>
        [HttpDelete("{id}/unavailability/{unavailabilityId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveUnavailability(
            int id,
            int unavailabilityId,
            CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.RemoveUnavailability(id, unavailabilityId, ct));
        }

        #endregion

        #region enabled
        /// <summary>
        /// Activa un servicio permitiendo que pueda recibir turnos.
        /// </summary>
        [HttpPatch("{id}/activate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Activate(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.Activate(id, ct));
        }
        /// <summary>
        /// Desactiva un servicio impidiendo que pueda recibir nuevos turnos.
        /// </summary>
        [HttpPatch("{id}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.Deactivate(id, ct));
        }
        #endregion
    }
}
