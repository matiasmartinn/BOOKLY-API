using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BOOKLY.Infrastructure.Persistence
{
    public sealed class BooklyDbContextFactory : IDesignTimeDbContextFactory<BooklyDbContext>
    {
        public BooklyDbContext CreateDbContext(string[] args)
        {
            var connectionString = ResolveConnectionString()
                ?? throw new InvalidOperationException("La configuración ConnectionStrings:BooklyDb es requerida para diseño.");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "La configuración ConnectionStrings:BooklyDb es requerida para diseño. Configúrala en appsettings.json o appsettings.Development.json.");
            }

            var normalizedConnectionString = SqlServerConnectionStringNormalizer.Normalize(connectionString);

            var optionsBuilder = new DbContextOptionsBuilder<BooklyDbContext>();
            optionsBuilder.UseSqlServer(normalizedConnectionString);

            return new BooklyDbContext(optionsBuilder.Options, NoOpDomainEventDispatcher.Instance);
        }

        private static string? ResolveConnectionString()
        {
            var basePath = ResolveConfigurationBasePath();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile(Path.Combine("BOOKLY.Api", "appsettings.json"), optional: true)
                .AddJsonFile(Path.Combine("BOOKLY.Api", "appsettings.Development.json"), optional: true)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            return configuration.GetConnectionString("BooklyDb");
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
