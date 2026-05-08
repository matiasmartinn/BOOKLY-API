using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Common.Security;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

        protected Result<int> GetAuthenticatedUserId()
        {
            var rawUserId =
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(rawUserId, out var userId) || userId <= 0)
                return Result<int>.Failure(Error.Unauthorized("El token no contiene un usuario válido."));

            return Result<int>.Success(userId);
        }

        protected Result<UserRole> GetAuthenticatedUserRole()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);

            return role switch
            {
                Roles.Admin => Result<UserRole>.Success(UserRole.Admin),
                Roles.Owner => Result<UserRole>.Success(UserRole.Owner),
                Roles.Secretary => Result<UserRole>.Success(UserRole.Secretary),
                _ => Result<UserRole>.Failure(Error.Unauthorized("El token no contiene un rol válido."))
            };
        }

        protected Result EnsureOwnerAccess(int ownerId)
        {
            var userIdResult = GetAuthenticatedUserId();
            if (userIdResult.IsFailure)
                return Result.Failure(userIdResult.Error);

            if (User.IsInRole(Roles.Admin))
                return Result.Success();

            return User.IsInRole(Roles.Owner) && userIdResult.Data == ownerId
                ? Result.Success()
                : Result.Failure(Error.Forbidden("No tienes permisos para operar sobre este owner."));
        }

        protected Result EnsureSelfOrAdmin(int userId)
        {
            var currentUserIdResult = GetAuthenticatedUserId();
            if (currentUserIdResult.IsFailure)
                return Result.Failure(currentUserIdResult.Error);

            if (User.IsInRole(Roles.Admin) || currentUserIdResult.Data == userId)
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
