using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Common.Security;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.UserAggregate;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/users")]
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var access = EnsureSelfOrAdmin(id);
            if (access.IsFailure)
                return HandleResult(access);

            return HandleResult(await _userService.GetUserById(id, ct));
        }

        [Authorize(Roles = Roles.Admin + "," + Roles.Owner)]
        [HttpPost("owners/{ownerId:int}/secretaries")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateSecretary(int ownerId, [FromBody] CreateSecretaryDto dto, CancellationToken ct)
        {
            var access = EnsureOwnerAccess(ownerId);
            if (access.IsFailure)
                return HandleResult(access);

            var result = await _userService.CreateSecretary(ownerId, dto, ct);
            return HandleCreated(result, nameof(GetById), new { id = result.Data?.Id });
        }

        [Authorize(Roles = Roles.Admin + "," + Roles.Owner)]
        [HttpGet("owners/{ownerId:int}/secretaries")]
        [ProducesResponseType(typeof(IReadOnlyCollection<SecretaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSecretariesByOwner(int ownerId, CancellationToken ct)
        {
            var access = EnsureOwnerAccess(ownerId);
            if (access.IsFailure)
                return HandleResult(access);

            return HandleResult(await _userService.GetSecretariesByOwner(ownerId, ct));
        }

        [Authorize(Roles = Roles.Admin + "," + Roles.Owner)]
        [HttpPatch("secretaries/{id:int}/activate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateSecretary(int id, CancellationToken ct)
        {
            return HandleResult(await _userService.ActivateSecretary(id, ct));
        }

        [Authorize(Roles = Roles.Admin + "," + Roles.Owner)]
        [HttpPatch("secretaries/{id:int}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateSecretary(int id, CancellationToken ct)
        {
            return HandleResult(await _userService.DeactivateSecretary(id, ct));
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto, CancellationToken ct)
        {
            var access = EnsureSelfOrAdmin(id);
            if (access.IsFailure)
                return HandleResult(access);

            return HandleResult(await _userService.UpdateUser(id, dto, ct));
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var access = EnsureSelfOrAdmin(id);
            if (access.IsFailure)
                return HandleResult(access);

            return HandleResult(await _userService.DeleteUser(id, ct));
        }
    }
}
