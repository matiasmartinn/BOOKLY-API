using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace BOOKLY.Infrastructure.Persistence
{
    public sealed class BooklyDbContextFactory : IDesignTimeDbContextFactory<BooklyDbContext>
    {
        public BooklyDbContext CreateDbContext(string[] args)
        {
            var connectionString = ResolveConnectionString()
                ?? throw new InvalidOperationException("Connection string 'BooklyDb' no encontrada para diseño.");
            var normalizedConnectionString = SqlServerConnectionStringNormalizer.Normalize(connectionString);

            var optionsBuilder = new DbContextOptionsBuilder<BooklyDbContext>();
            optionsBuilder.UseSqlServer(normalizedConnectionString);

            return new BooklyDbContext(optionsBuilder.Options, NoOpDomainEventDispatcher.Instance);
        }

        private static string? ResolveConnectionString()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__BooklyDb");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
                return fromEnvironment;

            var basePath = ResolveConfigurationBasePath();
            var booklyApiSettings = Path.Combine(basePath, "BOOKLY.Api", "appsettings.json");
            var rootSettings = Path.Combine(basePath, "appsettings.json");
            var settingsPath = File.Exists(booklyApiSettings) ? booklyApiSettings : rootSettings;

            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));

            if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
                && connectionStrings.TryGetProperty("BooklyDb", out var booklyDb)
                && booklyDb.ValueKind == JsonValueKind.String)
            {
                return booklyDb.GetString();
            }

            return null;
        }

        private static string ResolveConfigurationBasePath()
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "BOOKLY.Api", "appsettings.json")))
                    return current.FullName;

                if (File.Exists(Path.Combine(current.FullName, "appsettings.json")))
                    return current.FullName;

                current = current.Parent;
            }

            throw new InvalidOperationException("No se pudo localizar appsettings.json para las migraciones.");
        }

        private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
        {
            public static readonly NoOpDomainEventDispatcher Instance = new();

            public Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
                => Task.CompletedTask;
        }
    }
}
