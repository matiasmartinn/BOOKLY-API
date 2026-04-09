using BOOKLY.Application.Common.Security;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.UserAggregate;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Route("api/admins")]
    public class AdminsController : BaseController
    {
        private readonly IUserService _userService;

        public AdminsController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost("invite")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Invite([FromBody] InviteAdminDto dto, CancellationToken ct)
        {
            var result = await _userService.InviteAdmin(dto, ct);
            return result.IsSuccess
                ? CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = result.Data?.Id }, result.Data)
                : HandleResult(result);
        }

        [AllowAnonymous]
        [HttpPost("complete-invitation")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteInvitation([FromBody] CompleteAdminInvitationDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.CompleteAdminInvitation(dto, ct));
        }
    }
}
