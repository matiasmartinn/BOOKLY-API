using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Common.Security;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BOOKLY.Api.Security
{
    public sealed class CurrentUserContext : ICurrentUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
        public bool IsAdmin => Principal?.IsInRole(Roles.Admin) ?? false;
        public bool IsOwner => Principal?.IsInRole(Roles.Owner) ?? false;
        public bool IsSecretary => Principal?.IsInRole(Roles.Secretary) ?? false;

        public Result<int> GetUserId()
        {
            var rawUserId =
                Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(rawUserId, out var userId) || userId <= 0)
                return Result<int>.Failure(Error.Unauthorized("El token no contiene un usuario válido."));

            return Result<int>.Success(userId);
        }

        public Result<UserRole> GetRole()
        {
            var role = Principal?.FindFirstValue(ClaimTypes.Role);

            return role switch
            {
                Roles.Admin => Result<UserRole>.Success(UserRole.Admin),
                Roles.Owner => Result<UserRole>.Success(UserRole.Owner),
                Roles.Secretary => Result<UserRole>.Success(UserRole.Secretary),
                _ => Result<UserRole>.Failure(Error.Unauthorized("El token no contiene un rol válido."))
            };
        }

        public Result<int> ResolveOwnerId(int? requestedOwnerId = null)
        {
            var userId = GetUserId();
            if (userId.IsFailure)
                return userId;

            if (IsOwner)
            {
                // Tokens emitidos antes de incluir el claim siguen siendo válidos:
                // para un Owner, su ownerId es su propio userId.
                var ownerClaim = Principal?.FindFirstValue(BooklyClaims.OwnerId);
                var ownOwnerId = int.TryParse(ownerClaim, out var claimOwnerId) && claimOwnerId > 0
                    ? claimOwnerId
                    : userId.Data;

                if (requestedOwnerId.HasValue && requestedOwnerId.Value != ownOwnerId)
                    return Result<int>.Failure(Error.Forbidden("No tienes permisos para operar sobre este owner."));

                return Result<int>.Success(ownOwnerId);
            }

            if (IsAdmin)
            {
                return requestedOwnerId is > 0
                    ? Result<int>.Success(requestedOwnerId.Value)
                    : Result<int>.Failure(Error.Validation("Debes indicar un ownerId."));
            }

            return Result<int>.Failure(Error.Forbidden("No tienes permisos para operar sobre este owner."));
        }
    }
}
