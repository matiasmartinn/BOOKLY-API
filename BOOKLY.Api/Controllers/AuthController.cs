using BOOKLY.Application.Common;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AuthAggregate.DTOs;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly AuthOptions _authOptions;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            IAuthService authService,
            IUserService userService,
            IOptions<AuthOptions> authOptions,
            IWebHostEnvironment environment)
        {
            _authService = authService;
            _userService = userService;
            _authOptions = authOptions.Value;
            _environment = environment;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            return HandleAuthResult(await _authService.Login(request, ct));
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh(CancellationToken ct)
        {
            var refreshToken = Request.Cookies[GetRefreshTokenCookieName()];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return HandleResult(
                    Result<LoginResponse>.Failure(Error.Unauthorized("El refresh token es requerido.")));
            }

            var result = await _authService.Refresh(refreshToken, ct);
            if (result.IsFailure)
            {
                DeleteRefreshTokenCookie();
            }

            return HandleAuthResult(result);
        }   

        [AllowAnonymous]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var refreshToken = Request.Cookies[GetRefreshTokenCookieName()];
            var result = string.IsNullOrWhiteSpace(refreshToken)
                ? Result.Success()
                : await _authService.Logout(refreshToken, ct);

            DeleteRefreshTokenCookie();
            return HandleResult(result);
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

        private IActionResult HandleAuthResult(Result<AuthResult> result)
        {
            if (result.IsFailure)
            {
                return HandleResult(Result<LoginResponse>.Failure(result.Error));
            }

            AppendRefreshTokenCookie(result.Data!.RefreshToken);
            return HandleResult(Result<LoginResponse>.Success(result.Data.Response));
        }

        private void AppendRefreshTokenCookie(string refreshToken)
        {
            Response.Cookies.Append(
                GetRefreshTokenCookieName(),
                refreshToken,
                CreateRefreshTokenCookieOptions(includeLifetime: true));
        }

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete(
                GetRefreshTokenCookieName(),
                CreateRefreshTokenCookieOptions(includeLifetime: false));
        }

        private CookieOptions CreateRefreshTokenCookieOptions(bool includeLifetime)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = UseSecureCookies(),
                SameSite = GetCookieSameSite(),
                Path = GetRefreshTokenCookiePath(),
                IsEssential = true
            };

            if (includeLifetime)
            {
                var lifetime = TimeSpan.FromDays(GetRefreshTokenDays());
                options.Expires = DateTimeOffset.UtcNow.Add(lifetime);
                options.MaxAge = lifetime;
            }

            return options;
        }

        private string GetRefreshTokenCookieName()
            => string.IsNullOrWhiteSpace(_authOptions.RefreshTokenCookieName)
                ? AuthOptions.DefaultRefreshTokenCookieName
                : _authOptions.RefreshTokenCookieName;

        private string GetRefreshTokenCookiePath()
            => string.IsNullOrWhiteSpace(_authOptions.RefreshTokenCookiePath)
                ? AuthOptions.DefaultRefreshTokenCookiePath
                : _authOptions.RefreshTokenCookiePath;

        private int GetRefreshTokenDays()
            => _authOptions.RefreshTokenDays > 0
                ? _authOptions.RefreshTokenDays
                : AuthOptions.DefaultRefreshTokenDays;

        private bool UseSecureCookies()
            => _authOptions.UseSecureCookies ?? !_environment.IsDevelopment();

        private SameSiteMode GetCookieSameSite()
        {
            var configuredSameSite = _authOptions.CookieSameSite;
            if (string.IsNullOrWhiteSpace(configuredSameSite))
            {
                configuredSameSite = _environment.IsDevelopment()
                    ? AuthOptions.DefaultCookieSameSite
                    : nameof(SameSiteMode.None);
            }

            return Enum.TryParse<SameSiteMode>(configuredSameSite, ignoreCase: true, out var sameSite)
                ? sameSite
                : SameSiteMode.Lax;
        }
    }
}
