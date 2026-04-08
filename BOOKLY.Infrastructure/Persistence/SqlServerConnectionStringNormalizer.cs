using Microsoft.Data.SqlClient;

namespace BOOKLY.Infrastructure.Persistence
{
    internal static class SqlServerConnectionStringNormalizer
    {
        public static string Normalize(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            if (!ContainsKeyword(connectionString, "Encrypt"))
                builder.Encrypt = false;

            if (!ContainsKeyword(connectionString, "TrustServerCertificate"))
                builder.TrustServerCertificate = true;

            return builder.ConnectionString;
        }

        private static bool ContainsKeyword(string connectionString, string keyword)
            => connectionString.IndexOf($"{keyword}=", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
