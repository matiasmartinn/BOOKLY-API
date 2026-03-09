using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;

        public UserController (IUserService userService)
        {
            _userService = userService;
        }
        /// <summary>
        /// Recupera un usuario por su identificador desde la capa de aplicación.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            return HandleResult(await _userService.GetUserById(id, ct));
        }

        /// <summary>
        /// Crea un nuevo owner.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RegisterOwner([FromBody] CreateUserDto dto, CancellationToken ct)
        {
            var result = await _userService.RegisterOwner(dto, ct);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
                : HandleResult(result);
        }
        /// <summary>
        /// Actualiza los datos de un usuario existente aplicando validaciones de dominio.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto, CancellationToken ct)
        {
            return HandleResult(await _userService.UpdateUser(id, dto, ct));
        }
        /// <summary>
        /// Elimina un usuario del sistema según las reglas definidas en la capa de aplicación.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            return HandleResult(await _userService.DeleteUser(id, ct));
        }
    }
}
