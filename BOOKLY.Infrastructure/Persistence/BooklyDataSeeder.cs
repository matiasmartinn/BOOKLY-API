using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.ValueObjects;
using BOOKLY.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainEmail = BOOKLY.Domain.SharedKernel.Email;

namespace BOOKLY.Infrastructure.Persistence
{
    public static class BooklyDataSeeder
    {
        private const string InitialAdminEmail = "admin@bookly.local";
        private const string InitialAdminPassword = "Admin123!";

        private static readonly string[] InitialServiceTypes =
        [
            "Peluquería",
            "Barbería",
            "Psicología",
            "Kinesiología",
            "Nutrición",
            "Odontología",
            "Dermatología",
            "Masajes",
            "Manicura y uñas",
            "Depilación",
            "Estética facial",
            "Estética corporal",
            "Entrenamiento personal",
            "Clases particulares",
            "Consultoría profesional",
            "Asesoría legal",
            "Asesoría contable",
            "Reparación técnica",
            "Veterinaria",
            "Fotografía",
            "Maquillaje profesional",
            "Tatuajes y piercing",
            "Limpieza del hogar",
            "Coaching",
            "Terapias alternativas"
        ];

        public static async Task SeedBooklyDataAsync(this IServiceProvider services, CancellationToken ct = default)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

            await SeedServiceTypes(dbContext, ct);
            await SeedInitialAdmin(dbContext, passwordHasher, dateTimeProvider, ct);
        }

        private static async Task SeedServiceTypes(BooklyDbContext dbContext, CancellationToken ct)
        {
            var existingNames = await dbContext.ServiceTypes
                .AsNoTracking()
                .Select(serviceType => serviceType.Name)
                .ToListAsync(ct);

            var existingNameSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingServiceTypes = InitialServiceTypes
                .Where(name => !existingNameSet.Contains(name))
                .Select(name => ServiceType.Create(name))
                .ToList();

            if (missingServiceTypes.Count == 0)
                return;

            await dbContext.ServiceTypes.AddRangeAsync(missingServiceTypes, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        private static async Task SeedInitialAdmin(
            BooklyDbContext dbContext,
            IPasswordHasher passwordHasher,
            IDateTimeProvider dateTimeProvider,
            CancellationToken ct)
        {
            var adminExists = await dbContext.Users
                .AsNoTracking()
                .AnyAsync(user => user.Email.Value == InitialAdminEmail, ct);

            if (adminExists)
                return;

            var admin = User.CreateAdmin(
                PersonName.Create("Admin", "Bookly"),
                DomainEmail.Create(InitialAdminEmail),
                Password.FromHash(passwordHasher.Hash(InitialAdminPassword)),
                dateTimeProvider.NowArgentina());

            await dbContext.Users.AddAsync(admin, ct);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
