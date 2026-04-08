using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AdminAggregate.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Route("api/admin/dashboard")]
    public sealed class AdminDashboardController : BaseController
    {
        private readonly IAdminDashboardService _adminDashboardService;

        public AdminDashboardController(IAdminDashboardService adminDashboardService)
        {
            _adminDashboardService = adminDashboardService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDashboard([FromQuery] AdminDashboardQueryDto dto, CancellationToken ct)
        {
            return HandleResult(await _adminDashboardService.GetDashboard(dto, ct));
        }
    }
}
