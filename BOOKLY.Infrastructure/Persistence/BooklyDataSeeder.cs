using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DomainEmail = BOOKLY.Domain.SharedKernel.Email;

namespace BOOKLY.Infrastructure.Persistence
{
    public static class BooklyDataSeeder
    {
        private static readonly SeedServiceType[] InitialServiceTypes =
        [
            new("Peluquería", "#E11D48", "scissors"),
            new("Barbería", "#0F766E", "scissors"),
            new("Psicología", "#7C3AED", "heart-pulse"),
            new("Kinesiología", "#0284C7", "heart-pulse"),
            new("Nutrición", "#16A34A", "heart-pulse"),
            new("Odontología", "#0891B2", "heart-pulse"),
            new("Masajes", "#D97706", "sparkles"),
            new("Manicura y uñas", "#DB2777", "sparkles"),
            new("Entrenamiento personal", "#EA580C", "dumbbell"),
            new("Clases particulares", "#2563EB", "book-open"),
            new("Asesoría profesional", "#475569", "briefcase"),
            new("Terapias alternativas", "#9333EA", "sparkles")
        ];

        public static async Task SeedBooklyDataAsync(this IServiceProvider services, CancellationToken ct = default)
        {
            using var scope = services.CreateScope();

            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

            await SeedServiceTypes(dbContext, ct);
            await SeedInitialAdmin(dbContext, passwordHasher, dateTimeProvider, configuration, ct);
        }

        private static async Task SeedServiceTypes(BooklyDbContext dbContext, CancellationToken ct)
        {
            var serviceTypes = await dbContext.ServiceTypes
                .ToListAsync(ct);

            var serviceTypesByName = serviceTypes
                .ToDictionary(serviceType => serviceType.Name, StringComparer.OrdinalIgnoreCase);

            var changed = false;

            foreach (var seedItem in InitialServiceTypes)
            {
                if (serviceTypesByName.TryGetValue(seedItem.Name, out var serviceType))
                {
                    if (serviceType.ColorHex != seedItem.ColorHex || serviceType.IconKey != seedItem.IconKey)
                    {
                        serviceType.ChangeVisualIdentity(seedItem.ColorHex, seedItem.IconKey);
                        changed = true;
                    }

                    continue;
                }

                await dbContext.ServiceTypes.AddAsync(
                    ServiceType.Create(seedItem.Name, colorHex: seedItem.ColorHex, iconKey: seedItem.IconKey),
                    ct);

                changed = true;
            }

            if (changed)
                await dbContext.SaveChangesAsync(ct);
        }

        private static async Task SeedInitialAdmin(
            BooklyDbContext dbContext,
            IPasswordHasher passwordHasher,
            IDateTimeProvider dateTimeProvider,
            IConfiguration configuration,
            CancellationToken ct)
        {
            var adminEmail = configuration["Seed:AdminEmail"];
            var adminPassword = configuration["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                return;

            var adminExists = await dbContext.Users
                .AsNoTracking()
                .AnyAsync(user => user.Email.Value == adminEmail, ct);

            if (adminExists)
                return;

            var admin = User.CreateAdmin(
                PersonName.Create("Admin", "Bookly"),
                DomainEmail.Create(adminEmail),
                Password.FromHash(passwordHasher.Hash(adminPassword)),
                dateTimeProvider.NowArgentina());

            await dbContext.Users.AddAsync(admin, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        private sealed record SeedServiceType(string Name, string ColorHex, string IconKey);
    }
}