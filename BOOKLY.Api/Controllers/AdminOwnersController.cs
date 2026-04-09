using BOOKLY.Application.Common.Security;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AdminAggregate.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    [Route("api/admin/owners")]
    public sealed class AdminOwnersController : BaseController
    {
        private readonly IAdminOwnersService _adminOwnersService;

        public AdminOwnersController(IAdminOwnersService adminOwnersService)
        {
            _adminOwnersService = adminOwnersService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(AdminPagedResultDto<AdminOwnerListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetOwners([FromQuery] AdminOwnersQueryDto dto, CancellationToken ct)
        {
            return HandleResult(await _adminOwnersService.GetOwners(dto, ct));
        }

        [HttpPost("{id:int}/enable")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Enable(int id, CancellationToken ct)
        {
            return HandleResult(await _adminOwnersService.EnableOwner(id, ct));
        }

        [HttpPost("{id:int}/disable")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Disable(int id, CancellationToken ct)
        {
            return HandleResult(await _adminOwnersService.DisableOwner(id, ct));
        }
    }
}
