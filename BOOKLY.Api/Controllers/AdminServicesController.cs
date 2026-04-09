using BOOKLY.Application.Common.Security;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AdminAggregate.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    [Route("api/admin/services")]
    public sealed class AdminServicesController : BaseController
    {
        private readonly IAdminServicesService _adminServicesService;

        public AdminServicesController(IAdminServicesService adminServicesService)
        {
            _adminServicesService = adminServicesService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(AdminPagedResultDto<AdminServiceListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetServices([FromQuery] AdminServicesQueryDto dto, CancellationToken ct)
        {
            return HandleResult(await _adminServicesService.GetServices(dto, ct));
        }

        [HttpPost("{id:int}/enable")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Enable(int id, CancellationToken ct)
        {
            return HandleResult(await _adminServicesService.EnableService(id, ct));
        }

        [HttpPost("{id:int}/disable")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Disable(int id, CancellationToken ct)
        {
            return HandleResult(await _adminServicesService.DisableService(id, ct));
        }
    }
}
