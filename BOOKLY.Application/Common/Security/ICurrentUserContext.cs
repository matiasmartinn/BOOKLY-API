using BOOKLY.Application.Common.Models;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;

namespace BOOKLY.Application.Common.Security
{
    /// <summary>
    /// Expone la identidad del usuario autenticado derivada de los claims del token,
    /// para que los endpoints no dependan de identificadores enviados por el cliente.
    /// </summary>
    public interface ICurrentUserContext
    {
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
        bool IsOwner { get; }
        bool IsSecretary { get; }

        Result<int> GetUserId();
        Result<UserRole> GetRole();

        /// <summary>
        /// Deriva el ownerId efectivo de la operación:
        /// Owner → siempre su propio id (si el cliente envía otro distinto, se rechaza);
        /// Admin → usa el ownerId solicitado (requerido);
        /// otros roles → prohibido.
        /// </summary>
        Result<int> ResolveOwnerId(int? requestedOwnerId = null);
    }
}
