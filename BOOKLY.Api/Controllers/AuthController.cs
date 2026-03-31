using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : BaseController
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.Login(dto, ct));
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] CreateUserDto dto, CancellationToken ct)
        {
            var result = await _userService.RegisterOwner(dto, ct);
            return result.IsSuccess
                ? CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = result.Data?.Id }, result.Data)
                : HandleResult(result);
        }

        [HttpPost("confirm-email")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.ConfirmEmail(dto, ct));
        }

        [HttpPost("resend-confirmation")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailConfirmationDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.ResendEmailConfirmation(dto, ct));
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ForgotPassword([FromBody] RequestPasswordResetDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.RequestPasswordReset(dto, ct));
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.ResetPassword(dto, ct));
        }

        [HttpPost("secretary-invitations/complete")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteSecretaryInvitation([FromBody] CompleteSecretaryInvitationDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.CompleteInvitation(dto, ct));
        }
    }
}
