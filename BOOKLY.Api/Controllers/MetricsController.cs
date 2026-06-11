using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Common.Security;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.MetricsAggregate.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = Roles.Admin + "," + Roles.Owner)]
    [Route("api/metrics")]
    public sealed class MetricsController : BaseController
    {
        private readonly IMetricsService _metricsService;

        public MetricsController(IMetricsService metricsService)
        {
            _metricsService = metricsService;
        }

        /// <summary>
        /// Obtiene metricas de turnos para reporting y analitica.
        /// </summary>
        [HttpGet("appointments")]
        [ProducesResponseType(typeof(AppointmentMetricsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppointmentMetrics([FromQuery] AppointmentMetricsQueryDto dto, CancellationToken ct)
        {
            // Owner: el ownerId efectivo siempre sale del token.
            // Admin: puede consultar un owner puntual o métricas globales (sin ownerId).
            if (CurrentUser.IsOwner)
            {
                var resolvedOwnerId = ResolveOwnerId(dto.OwnerId);
                if (resolvedOwnerId.IsFailure)
                    return HandleResult(Result.Failure(resolvedOwnerId.Error));

                dto = dto with { OwnerId = resolvedOwnerId.Data };
            }

            return HandleResult(await _metricsService.GetAppointmentMetrics(dto, ct));
        }
    }
}
