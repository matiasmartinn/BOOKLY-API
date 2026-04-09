using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Common.Security;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Application.Services.ServiceAggregate.DTOs;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/appointments")]
    public class AppointmentsController : BaseController
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IServiceApplicationService _serviceApplicationService;

        public AppointmentsController(
            IAppointmentService appointmentService,
            IServiceApplicationService serviceApplicationService)
        {
            _appointmentService = appointmentService;
            _serviceApplicationService = serviceApplicationService;
        }

        #region Queries

        /// <summary>
        /// Obtiene un turno por identificador.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var access = await EnsureAppointmentPermission(id, SecretaryPermission.ViewAppointments, ct);
            if (access.IsFailure)
                return HandleResult(Result<AppointmentDto>.Failure(access.Error));

            return Ok(access.Data);
        }

        /// <summary>
        /// Obtiene todos los turnos asociados a un servicio específico.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByService([FromQuery] int serviceId, CancellationToken ct)
        {
            var access = await EnsureServicePermission(serviceId, SecretaryPermission.ViewAppointments, ct);
            if (access.IsFailure)
                return HandleResult(Result<IReadOnlyCollection<AppointmentDto>>.Failure(access.Error));

            return HandleResult(await _appointmentService.GetByService(serviceId, ct));
        }

        /// <summary>
        /// Obtiene los turnos de un servicio específico filtrados por una fecha determinada.
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByServiceAndDate([FromQuery] int serviceId, [FromQuery] DateOnly date, CancellationToken ct)
        {
            var access = await EnsureServicePermission(serviceId, SecretaryPermission.ViewAppointments, ct);
            if (access.IsFailure)
                return HandleResult(Result<IReadOnlyCollection<AppointmentSummaryDto>>.Failure(access.Error));

            return HandleResult(await _appointmentService.GetByServiceAndDate(serviceId, date, ct));
        }

        /// <summary>
        /// Obtiene los turnos del día para un owner o servicio específico.
        /// </summary>
        [HttpGet("day")]
        [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByDay([FromQuery] AppointmentDayQueryDto dto, CancellationToken ct)
        {
            var access = await EnsureAppointmentQueryAccess(dto.OwnerId, dto.ServiceId, ct);
            if (access.IsFailure)
                return HandleResult(access);

            return HandleResult(await _appointmentService.GetByDay(dto, ct));
        }

        /// <summary>
        /// Consulta turnos por owner o servicio, con filtros por rango, estado y cliente.
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Search([FromQuery] AppointmentQueryDto dto, CancellationToken ct)
        {
            var access = await EnsureAppointmentQueryAccess(dto.OwnerId, dto.ServiceId, ct);
            if (access.IsFailure)
                return HandleResult(access);

            return HandleResult(await _appointmentService.SearchAppointments(dto, ct));
        }

        /// <summary>
        /// Obtiene la traza de estados de los turnos de un servicio.
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentStatusHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHistoryByService([FromQuery] int serviceId, CancellationToken ct)
        {
            var access = await EnsureServicePermission(serviceId, SecretaryPermission.ViewAppointments, ct);
            if (access.IsFailure)
                return HandleResult(Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>.Failure(access.Error));

            return HandleResult(await _appointmentService.GetHistoryByService(serviceId, ct));
        }

        /// <summary>
        /// Obtiene la traza de estados de un turno.
        /// </summary>
        [HttpGet("{id:int}/history")]
        [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentStatusHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHistory(int id, CancellationToken ct)
        {
            var access = await EnsureAppointmentPermission(id, SecretaryPermission.ViewAppointments, ct);
            if (access.IsFailure)
                return HandleResult(Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>.Failure(access.Error));

            return HandleResult(await _appointmentService.GetHistoryByAppointment(id, ct));
        }

        #endregion

        #region Commands

        /// <summary>
        /// Crea un nuevo turno para un servicio.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto, CancellationToken ct)
        {
            var access = await EnsureServicePermission(dto.ServiceId, SecretaryPermission.CreateAppointments, ct);
            if (access.IsFailure)
                return HandleResult(Result<AppointmentDto>.Failure(access.Error));

            var currentUserId = GetAuthenticatedUserId();
            if (currentUserId.IsFailure)
                return HandleResult(Result<AppointmentDto>.Failure(currentUserId.Error));

            dto = dto with { UserId = currentUserId.Data };
            var result = await _appointmentService.CreateAppointment(dto, ct);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
                : HandleResult(result);
        }

        /// <summary>
        /// Actualiza la información del cliente asociada al turno.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAppointmentDto dto, CancellationToken ct)
        {
            var access = await EnsureAppointmentPermission(id, SecretaryPermission.EditAppointments, ct);
            if (access.IsFailure)
                return HandleResult(Result<AppointmentDto>.Failure(access.Error));

            return HandleResult(await _appointmentService.UpdateAppointmentInformation(id, dto, ct));
        }

        /// <summary>
        /// Reprograma un turno existente.
        /// </summary>
        [HttpPatch("{id:int}/reschedule")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleAppointmentDto dto, CancellationToken ct)
        {
            var access = await EnsureAppointmentPermission(id, SecretaryPermission.RescheduleAppointments, ct);
            if (access.IsFailure)
                return HandleResult(Result<AppointmentDto>.Failure(access.Error));

            return HandleResult(await _appointmentService.RescheduleAppointment(id, dto, ct));
        }

        #endregion

        #region Status

        /// <summary>
        /// Cancela un turno existente.
        /// </summary>
        [HttpPatch("{id:int}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelAppointmentDto dto, CancellationToken ct)
        {
            var access = await EnsureAppointmentPermission(id, SecretaryPermission.CancelAppointments, ct);
            if (access.IsFailure)
                return HandleResult(Result.Failure(access.Error));

            var currentUserId = GetAuthenticatedUserId();
            if (currentUserId.IsFailure)
                return HandleResult(Result.Failure(currentUserId.Error));

            dto = dto with { UserId = currentUserId.Data };
            return HandleResult(await _appointmentService.MarkAsCancel(id, dto, ct));
        }

        /// <summary>
        /// Marca un turno como asistido.
        /// </summary>
        [HttpPatch("{id:int}/attended")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsAttended(int id, CancellationToken ct)
        {
            var access = await EnsureAppointmentPermission(id, SecretaryPermission.MarkAttendance, ct);
            if (access.IsFailure)
                return HandleResult(Result.Failure(access.Error));

            var currentUserId = GetAuthenticatedUserId();
            if (currentUserId.IsFailure)
                return HandleResult(Result.Failure(currentUserId.Error));

            return HandleResult(await _appointmentService.MarkAsAttended(id, currentUserId.Data, ct));
        }

        /// <summary>
        /// Marca un turno como no asistido.
        /// </summary>
        [HttpPatch("{id:int}/no-show")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsNoShow(int id, CancellationToken ct)
        {
            var access = await EnsureAppointmentPermission(id, SecretaryPermission.MarkAttendance, ct);
            if (access.IsFailure)
                return HandleResult(Result.Failure(access.Error));

            var currentUserId = GetAuthenticatedUserId();
            if (currentUserId.IsFailure)
                return HandleResult(Result.Failure(currentUserId.Error));

            return HandleResult(await _appointmentService.MarkAsNoShow(id, currentUserId.Data, ct));
        }

        #endregion

        private async Task<Result> EnsureAppointmentQueryAccess(int? ownerId, int? serviceId, CancellationToken ct)
        {
            if (ownerId.HasValue)
                return EnsureOwnerAccess(ownerId.Value);

            if (serviceId.HasValue)
            {
                var serviceAccess = await EnsureServicePermission(
                    serviceId.Value,
                    SecretaryPermission.ViewAppointments,
                    ct);
                return serviceAccess.IsFailure
                    ? Result.Failure(serviceAccess.Error)
                    : Result.Success();
            }

            return Result.Failure(Error.Validation("Debes indicar un ownerId o un serviceId."));
        }

        private async Task<Result<AppointmentDto>> EnsureAppointmentPermission(
            int appointmentId,
            SecretaryPermission permission,
            CancellationToken ct)
        {
            var appointmentResult = await _appointmentService.GetById(appointmentId, ct);
            if (appointmentResult.IsFailure)
                return Result<AppointmentDto>.Failure(appointmentResult.Error);

            var serviceAccess = await EnsureServicePermission(appointmentResult.Data!.ServiceId, permission, ct);
            if (serviceAccess.IsFailure)
                return Result<AppointmentDto>.Failure(serviceAccess.Error);

            return appointmentResult;
        }

        private async Task<Result<ServiceDto>> EnsureServicePermission(
            int serviceId,
            SecretaryPermission permission,
            CancellationToken ct)
        {
            var serviceAccess = await EnsureServiceAccess(serviceId, allowSecretary: true, ct);
            if (serviceAccess.IsFailure)
                return serviceAccess;

            if (!User.IsInRole(Roles.Secretary))
                return serviceAccess;

            var currentUserId = GetAuthenticatedUserId();
            if (currentUserId.IsFailure)
                return Result<ServiceDto>.Failure(currentUserId.Error);

            var service = serviceAccess.Data!;
            var permissions = service.SecretaryPermissions
                .FirstOrDefault(item => item.SecretaryId == currentUserId.Data)?
                .Permissions ?? [];

            if (permissions.Contains(permission))
                return serviceAccess;

            return Result<ServiceDto>.Failure(Error.Forbidden(BuildPermissionDeniedMessage(permission)));
        }

        private async Task<Result<ServiceDto>> EnsureServiceAccess(int serviceId, bool allowSecretary, CancellationToken ct)
        {
            var serviceResult = await _serviceApplicationService.GetServiceById(serviceId, ct);
            if (serviceResult.IsFailure)
                return Result<ServiceDto>.Failure(serviceResult.Error);

            var currentUserId = GetAuthenticatedUserId();
            if (currentUserId.IsFailure)
                return Result<ServiceDto>.Failure(currentUserId.Error);

            var service = serviceResult.Data!;

            if (User.IsInRole(Roles.Admin))
                return serviceResult;

            if (User.IsInRole(Roles.Owner) && service.OwnerId == currentUserId.Data)
                return serviceResult;

            if (allowSecretary &&
                User.IsInRole(Roles.Secretary) &&
                service.SecretaryIds.Any(secretaryId => secretaryId == currentUserId.Data))
            {
                return serviceResult;
            }

            return Result<ServiceDto>.Failure(Error.Forbidden("No tienes permisos para acceder a este servicio."));
        }

        private static string BuildPermissionDeniedMessage(SecretaryPermission permission)
        {
            return permission switch
            {
                SecretaryPermission.ViewAppointments => "No tienes permisos para ver turnos en este servicio.",
                SecretaryPermission.CreateAppointments => "No tienes permisos para crear turnos en este servicio.",
                SecretaryPermission.EditAppointments => "No tienes permisos para editar turnos en este servicio.",
                SecretaryPermission.CancelAppointments => "No tienes permisos para cancelar turnos en este servicio.",
                SecretaryPermission.RescheduleAppointments => "No tienes permisos para reprogramar turnos en este servicio.",
                SecretaryPermission.MarkAttendance => "No tienes permisos para registrar asistencia en este servicio.",
                SecretaryPermission.ManageSchedules => "No tienes permisos para gestionar horarios en este servicio.",
                _ => "No tienes permisos para realizar esta acción en este servicio.",
            };
        }
    }
}
