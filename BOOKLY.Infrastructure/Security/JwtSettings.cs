using System.Text;

namespace BOOKLY.Infrastructure.Security
{
    public sealed class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string SecretKey { get; init; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; init; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Issuer))
                throw new InvalidOperationException("La configuración Jwt:Issuer es requerida.");

            if (string.IsNullOrWhiteSpace(Audience))
                throw new InvalidOperationException("La configuración Jwt:Audience es requerida.");

            if (string.IsNullOrWhiteSpace(SecretKey))
                throw new InvalidOperationException("La configuración Jwt:SecretKey es requerida.");

            if (Encoding.UTF8.GetByteCount(SecretKey) < 32)
            {
                throw new InvalidOperationException(
                    "La configuración Jwt:SecretKey debe tener al menos 32 bytes para usar HS256.");
            }

            if (AccessTokenExpirationMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "La configuración Jwt:AccessTokenExpirationMinutes debe ser mayor a cero.");
            }
        }
    }
}
