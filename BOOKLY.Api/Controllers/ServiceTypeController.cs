using BOOKLY.Application.Common.Security;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.ServiceTypeAggregate.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/service-types")]
    public class ServiceTypeController : BaseController
    {
        private readonly IServiceTypeService _serviceTypeService;

        public ServiceTypeController(IServiceTypeService serviceTypeService)
        {
            _serviceTypeService = serviceTypeService;
        }

        #region Basic CRUD
        /// <summary>
        /// Obtiene todos los tipos de servicio registrados en el sistema.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ServiceTypeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.GetAll(ct));
        }

        /// <summary>
        /// Obtiene un tipo de servicio específico por su identificador.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.GetById(id, ct));
        }

        /// <summary>
        /// Obtiene un tipo de servicio por su identificador, incluyendo sus campos dinámicos configurados.
        /// </summary>
        [HttpGet("{id:int}/fields")]
        [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdWithFields(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.GetByIdWithFields(id, ct));
        }

        /// <summary>
        /// Crea un nuevo tipo de servicio con su configuración básica.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateServiceTypeDto dto, CancellationToken ct)
        {
            var result = await _serviceTypeService.CreateServiceType(dto, ct);
            return HandleCreated(result, nameof(GetById), new { id = result.Data?.Id });
        }

        /// <summary>
        /// Actualiza los datos principales de un tipo de servicio existente.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceTypeDto dto, CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.UpdateServiceType(id, dto, ct));
        }

        /// <summary>
        /// Elimina un tipo de servicio del sistema.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.DeleteServiceType(id, ct));
        }

        #endregion

        #region Fields
        /// <summary>
        /// Agrega un nuevo campo dinámico a un tipo de servicio.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{id:int}/fields")]
        [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddField(int id, [FromBody] AddServiceTypeFieldDto dto, CancellationToken ct)
        {
            var result = await _serviceTypeService.AddField(id, dto, ct);
            return HandleCreated(result, nameof(GetByIdWithFields), new { id });
        }

        /// <summary>
        /// Actualiza la configuración de un campo dinámico perteneciente a un tipo de servicio.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id:int}/fields/{fieldId:int}")]
        [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateField(int id, int fieldId, [FromBody] UpdateServiceTypeFieldDto dto, CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.UpdateField(id, fieldId, dto, ct));
        }

        /// <summary>
        /// Activa un campo dinámico previamente desactivado.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{id:int}/fields/{fieldId:int}/activate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateField(int id, int fieldId, CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.ActivateField(id, fieldId, ct));
        }

        /// <summary>
        /// Desactiva un campo dinámico de un tipo de servicio.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{id:int}/fields/{fieldId:int}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateField(int id, int fieldId, CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.RemoveField(id, fieldId, ct));
        }

        #endregion

        #region Options
        /// <summary>
        /// Agrega una nueva opción a un campo de tipo selección dentro de un tipo de servicio.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{id:int}/fields/{fieldId:int}/options")]
        [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddOption(int id, int fieldId, [FromBody] AddServiceTypeFieldOptionDto dto, CancellationToken ct)
        {
            var result = await _serviceTypeService.AddOption(id, fieldId, dto, ct);
            return HandleCreated(result, nameof(GetByIdWithFields), new { id });
        }

        /// <summary>
        /// Actualiza una opción existente de un campo perteneciente a un tipo de servicio.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id:int}/fields/{fieldId:int}/options/{optionId:int}")]
        [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateOption(int id, int fieldId, int optionId, [FromBody] UpdateServiceTypeFieldOptionDto dto, CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.UpdateOption(id, fieldId, optionId, dto, ct));
        }

        /// <summary>
        /// Elimina una opción de un campo perteneciente a un tipo de servicio.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id:int}/fields/{fieldId:int}/options/{optionId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveOption(int id, int fieldId, int optionId, CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.RemoveOption(id, fieldId, optionId, ct));
        }

        /// <summary>
        /// Activa una opción previamente desactivada de un campo.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{id:int}/fields/{fieldId:int}/options/{optionId:int}/activate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateOption(int id, int fieldId, int optionId, CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.ActivateOption(id, fieldId, optionId, ct));
        }

        /// <summary>
        /// Desactiva una opción de un campo perteneciente a un tipo de servicio.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{id:int}/fields/{fieldId:int}/options/{optionId:int}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateOption(int id, int fieldId, int optionId, CancellationToken ct)
        {
            return HandleResult(await _serviceTypeService.DeactivateOption(id, fieldId, optionId, ct));
        }

        #endregion
    }
}
