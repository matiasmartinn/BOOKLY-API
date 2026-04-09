using BOOKLY.Application.Common.Security;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Application.Services.ClientAggregate.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = Roles.Admin + "," + Roles.Owner)]
    [Route("api/clients")]
    public sealed class ClientsController : BaseController
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyCollection<ClientListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByOwner([FromQuery] int ownerId, [FromQuery] string? search, CancellationToken ct)
        {
            var access = EnsureOwnerAccess(ownerId);
            if (access.IsFailure)
                return HandleResult(access);

            return HandleResult(await _clientService.GetByOwner(ownerId, search, ct));
        }

        [HttpGet("detail")]
        [ProducesResponseType(typeof(ClientDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDetail([FromQuery] int ownerId, [FromQuery] string email, CancellationToken ct)
        {
            var access = EnsureOwnerAccess(ownerId);
            if (access.IsFailure)
                return HandleResult(access);

            return HandleResult(await _clientService.GetDetail(ownerId, email, ct));
        }

        [HttpGet("appointments")]
        [ProducesResponseType(typeof(IReadOnlyCollection<AppointmentListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppointmentHistory([FromQuery] int ownerId, [FromQuery] string email, CancellationToken ct)
        {
            var access = EnsureOwnerAccess(ownerId);
            if (access.IsFailure)
                return HandleResult(access);

            return HandleResult(await _clientService.GetAppointmentHistory(ownerId, email, ct));
        }
    }
}
