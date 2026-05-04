using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AuthAggregate.DTOs;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        public AuthController(
            IAuthService authService,
            IUserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            return HandleResult(await _authService.Login(request, ct));
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
        {
            return HandleResult(await _authService.Refresh(request, ct));
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
        {
            return HandleResult(await _authService.Logout(request.RefreshToken, ct));
        }

        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterOwnerResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] CreateUserDto dto, CancellationToken ct)
        {
            var result = await _userService.RegisterOwner(dto, ct);
            return HandleCreated(result, nameof(UsersController.GetById), "Users", new { id = result.Data?.User.Id });
        }

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.ConfirmEmail(dto, ct));
        }

        [AllowAnonymous]
        [HttpPost("resend-confirmation")]
        [ProducesResponseType(typeof(EmailDispatchResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailConfirmationDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.ResendEmailConfirmation(dto, ct));
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ForgotPassword([FromBody] RequestPasswordResetDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.RequestPasswordReset(dto, ct));
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.ResetPassword(dto, ct));
        }

        [AllowAnonymous]
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
