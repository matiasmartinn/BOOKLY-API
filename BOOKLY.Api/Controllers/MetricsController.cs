using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.MetricsAggregate.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
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
            return HandleResult(await _metricsService.GetAppointmentMetrics(dto, ct));
        }
    }
}
