using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;

namespace BOOKLY.Application.Interfaces
{
    public interface IServiceAuthorizationService
    {
        bool HasPermission(Service service, int userId, UserRole role, SecretaryPermission permission);
    }
}
