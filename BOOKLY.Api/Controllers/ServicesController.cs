using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AppointmentAggregate;
using BOOKLY.Application.Services.ServiceAggregate.DTOs;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Route("api/services")]
    public class ServicesController : BaseController
    {
        private readonly IServiceApplicationService _serviceApplicationService;

        public ServicesController(IServiceApplicationService serviceApplicationService)
        {
            _serviceApplicationService = serviceApplicationService;
        }

        #region Basic Crud
        /// <summary>
        /// Recupera un servicio junto con su configuración básica.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.GetServiceById(id, ct));
        }

        /// <summary>
        /// Recupera todos los servicios del usuario.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetServicesByOwner([FromQuery] int ownerId, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.GetServicesByOwner(ownerId, ct));
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
        [HttpPut("{id:int}")]
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
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct) 
        {
           return HandleResult(await _serviceApplicationService.DeleteService(id, ct));
        }
        #endregion

        #region Public Booking
        [HttpGet("{id:int}/public-booking")]
        [ProducesResponseType(typeof(ServicePublicBookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublicBooking(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.GetPublicBooking(id, ct));
        }

        [HttpPost("{id:int}/public-booking/enable")]
        [ProducesResponseType(typeof(ServicePublicBookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EnablePublicBooking(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.EnablePublicBooking(id, ct));
        }

        [HttpPost("{id:int}/public-booking/disable")]
        [ProducesResponseType(typeof(ServicePublicBookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DisablePublicBooking(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.DisablePublicBooking(id, ct));
        }

        [HttpPost("{id:int}/public-booking/regenerate")]
        [ProducesResponseType(typeof(ServicePublicBookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RegeneratePublicBooking(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.RegeneratePublicBooking(id, ct));
        }
        #endregion

        #region Schedules
        [HttpGet("{id:int}/schedules")]
        [ProducesResponseType(typeof(IEnumerable<ServiceScheduleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSchedules(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.GetSchedulesByService(id, ct));
        }

        [HttpGet("{id:int}/unavailabilities")]
        [ProducesResponseType(typeof(IEnumerable<ScheduleUnavailabilityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUnavailability(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.GetUnavailabilityByService(id, ct));
        }

        [HttpPut("{id:int}/secretaries")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetSecretaries(int id, [FromBody] SetSecretariesDto dto, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.SetSecretaries(id, dto, ct));
        }

        [HttpPut("{id:int}/secretaries/{secretaryId:int}/permissions/{permission}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GrantSecretaryPermission(
            int id,
            int secretaryId,
            SecretaryPermission permission,
            [FromQuery] int currentUserId,
            [FromQuery] UserRole currentUserRole,
            CancellationToken ct)
        {
            return HandleResult(
                await _serviceApplicationService.GrantSecretaryPermission(
                    id,
                    secretaryId,
                    permission,
                    currentUserId,
                    currentUserRole,
                    ct));
        }

        [HttpDelete("{id:int}/secretaries/{secretaryId:int}/permissions/{permission}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RevokeSecretaryPermission(
            int id,
            int secretaryId,
            SecretaryPermission permission,
            [FromQuery] int currentUserId,
            [FromQuery] UserRole currentUserRole,
            CancellationToken ct)
        {
            return HandleResult(
                await _serviceApplicationService.RevokeSecretaryPermission(
                    id,
                    secretaryId,
                    permission,
                    currentUserId,
                    currentUserRole,
                    ct));
        }

        /// <summary>
        /// Define o reemplaza la configuración de horarios disponibles de un servicio.
        /// </summary>
        [HttpPut("{id:int}/schedules")]
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
        [HttpGet("{id:int}/availability/slots")]
        [ProducesResponseType(typeof(List<DateTime>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAvailableSlots(int id,[FromQuery] DateOnly date, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.GetAvailableSlots(id, date, ct));
        }

        /// <summary>
        /// Calcula y devuelve las fechas con turnos disponibles para un servicio en un rango determinado.
        /// </summary>
        [HttpGet("{id:int}/availability/dates")]
        [ProducesResponseType(typeof(List<DateOnly>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAvailableDates(int id, [FromQuery] DateOnly from, [FromQuery ]DateOnly to, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.GetAvailableDates(id, from, to, ct));
        }

        #endregion

        #region UNAVAILABILITIES
        /// <summary>
        /// Registra una excepción de disponibilidad para un servicio en una fecha específica.
        /// </summary>
        [HttpPost("{id:int}/unavailabilities")]
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
        [HttpDelete("{id:int}/unavailabilities/{unavailabilityId:int}")]
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
        [HttpPatch("{id:int}/activate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Activate(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.Activate(id, ct));
        }
        /// <summary>
        /// Desactiva un servicio impidiendo que pueda recibir nuevos turnos.
        /// </summary>
        [HttpPatch("{id:int}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceApplicationService.Deactivate(id, ct));
        }
        #endregion
    }
}
