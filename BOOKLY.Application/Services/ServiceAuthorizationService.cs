using BOOKLY.Application.Interfaces;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;

namespace BOOKLY.Application.Services
{
    public sealed class ServiceAuthorizationService : IServiceAuthorizationService
    {
        public bool HasPermission(
            Service service,
            int userId,
            UserRole role,
            SecretaryPermission permission)
        {
            if (!Enum.IsDefined(typeof(SecretaryPermission), permission))
                return false;

            if(role == UserRole.Admin) 
                return true;

            if(role == UserRole.Owner && service.OwnerId == userId) 
                return true;

            if(role == UserRole.Secretary)
            {
                var secretary = service.ServiceSecretaries.FirstOrDefault(s => s.SecretaryId == userId);

                if(secretary == null)
                    return false;

                return secretary.HasPermission(permission);
            }

            return false;
        }
    }
}
