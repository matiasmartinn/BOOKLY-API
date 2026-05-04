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

            ErrorType.Domain => Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Regla de negocio violada"),

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
