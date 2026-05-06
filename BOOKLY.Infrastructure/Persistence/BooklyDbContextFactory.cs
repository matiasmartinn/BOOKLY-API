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
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("BooklyDb")
                ?? throw new InvalidOperationException(
                    "La configuración ConnectionStrings:BooklyDb es requerida para diseño. Configúrala en appsettings.Development.json.");

            var optionsBuilder = new DbContextOptionsBuilder<BooklyDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new BooklyDbContext(optionsBuilder.Options, NoOpDomainEventDispatcher.Instance);
        }

        private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
        {
            public static readonly NoOpDomainEventDispatcher Instance = new();

            public Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
                => Task.CompletedTask;
        }
    }
}
