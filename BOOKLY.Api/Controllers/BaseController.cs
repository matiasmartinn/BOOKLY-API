using BOOKLY.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess) return Ok(result.Data);
            return MapError(result.Error!);
        }

        protected IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess) return NoContent();
            return MapError(result.Error!);
        }

        protected IActionResult HandleCreated<T>(
            Result<T> result,
            string actionName,
            object? routeValues = null)
        {
            if (result.IsSuccess)
                return CreatedAtAction(actionName, routeValues, result.Data);

            return MapError(result.Error!);
        }

        private IActionResult MapError(Error error) => error.Type switch
        {
            ErrorType.NotFound => NotFound(new { error = error.Message }),
            ErrorType.Validation => BadRequest(new { error = error.Message }),
            ErrorType.Conflict => Conflict(new { error = error.Message }),
            ErrorType.Unauthorized => Unauthorized(new { error = error.Message }),
            ErrorType.Forbidden => base.StatusCode(StatusCodes.Status403Forbidden, new { error = error.Message }),
            _ => base.StatusCode(StatusCodes.Status500InternalServerError, new { error = error.Message }),
        };
    }
}
