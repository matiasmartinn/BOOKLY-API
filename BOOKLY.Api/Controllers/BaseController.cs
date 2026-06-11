using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Common.Security;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BOOKLY.Api.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        protected IActionResult HandleResult<T>(
            Result<T> result,
            int successStatusCode = StatusCodes.Status200OK)
        {
            if (result.IsFailure)
                return MapError(result.Error!);

            return successStatusCode == StatusCodes.Status200OK
                ? Ok(result.Data)
                : StatusCode(successStatusCode, result.Data);
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

        protected IActionResult HandleCreated<T>(
            Result<T> result,
            string actionName,
            string controllerName,
            object? routeValues = null)
        {
            if (result.IsSuccess)
                return CreatedAtAction(actionName, controllerName, routeValues, result.Data);

            return MapError(result.Error!);
        }

        protected IActionResult HandleCreated<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return StatusCode(StatusCodes.Status201Created, result.Data);

            return MapError(result.Error!);
        }

        protected ICurrentUserContext CurrentUser =>
            HttpContext.RequestServices.GetRequiredService<ICurrentUserContext>();

        protected Result<int> GetAuthenticatedUserId() => CurrentUser.GetUserId();

        protected Result<UserRole> GetAuthenticatedUserRole() => CurrentUser.GetRole();

        protected Result<int> ResolveOwnerId(int? ownerId = null) => CurrentUser.ResolveOwnerId(ownerId);

        protected Result EnsureSelfOrAdmin(int userId)
        {
            var currentUserIdResult = CurrentUser.GetUserId();
            if (currentUserIdResult.IsFailure)
                return Result.Failure(currentUserIdResult.Error);

            if (CurrentUser.IsAdmin || currentUserIdResult.Data == userId)
                return Result.Success();

            return Result.Failure(Error.Forbidden("No tienes permisos para operar sobre este usuario."));
        }

        private IActionResult MapError(Error error)
        {
            var instance = HttpContext.Request.Path.Value;

            return error.Type switch
            {
                ErrorType.NotFound => Problem(
                    type: "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
                    title: "Recurso no encontrado",
                    statusCode: StatusCodes.Status404NotFound,
                    detail: error.Message,
                    instance: instance),

                ErrorType.Validation => Problem(
                    type: "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                    title: "Error de validación",
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: error.Message,
                    instance: instance),

                ErrorType.Domain => Problem(
                    type: "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                    title: "Regla de negocio violada",
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: error.Message,
                    instance: instance),

                ErrorType.Conflict => Problem(
                    type: "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
                    title: "Conflicto",
                    statusCode: StatusCodes.Status409Conflict,
                    detail: error.Message,
                    instance: instance),

                ErrorType.Unauthorized => Problem(
                    type: "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
                    title: "No autorizado",
                    statusCode: StatusCodes.Status401Unauthorized,
                    detail: error.Message,
                    instance: instance),

                ErrorType.Forbidden => Problem(
                    type: "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4",
                    title: "Acceso denegado",
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: error.Message,
                    instance: instance),

                ErrorType.Unexpected => Problem(
                    type: "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                    title: "Error inesperado",
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: "Ocurrió un error inesperado.",
                    instance: instance),

                ErrorType.RateLimitExceeded => Problem(
                     type: "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
                    title: "Demasiadas solicitudes",
                    statusCode: StatusCodes.Status429TooManyRequests,
                    detail: error.Message,
                    instance: instance),

                _ => Problem(
                    type: "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                    title: "Error inesperado",
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: "Ocurrió un error inesperado.",
                    instance: instance),
            };
        }
    }
}
