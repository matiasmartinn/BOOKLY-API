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
            ErrorType.NotFound => Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status404NotFound,
                title: "Recurso no encontrado"),

            ErrorType.Validation => Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Error de validación"),

            ErrorType.Conflict => Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflicto"),

            ErrorType.Unauthorized => Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status401Unauthorized,
                title: "No autorizado"),

            ErrorType.Forbidden => Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status403Forbidden,
                title: "Acceso denegado"),

            _ => Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error inesperado"),
        };
    }
}
