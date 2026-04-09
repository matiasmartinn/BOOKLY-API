using BOOKLY.Domain.Aggregates.UserAggregate.Enums;

namespace BOOKLY.Application.Common.Security
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Owner = "Owner";
        public const string Secretary = "Secretary";

        public static string From(UserRole role)
            => role switch
            {
                UserRole.Admin => Admin,
                UserRole.Owner => Owner,
                UserRole.Secretary => Secretary,
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Rol no soportado.")
            };
    }
}
