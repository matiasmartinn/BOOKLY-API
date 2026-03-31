using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;
using BOOKLY.Infrastructure.Email;
using BOOKLY.Infrastructure.Persistence;
using BOOKLY.Infrastructure.Persistence.Repositories;
using BOOKLY.Infrastructure.Repositories;
using BOOKLY.Infrastructure.Security;
using BOOKLY.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BOOKLY.Infrastructure
{
    public static class InfraDependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("BooklyDb")
                ?? throw new InvalidOperationException("Connection string 'BooklyDb' no encontrada.");

            services.AddDbContext<BooklyDbContext>(options =>
            {
                options.UseSqlServer(connectionString);

#if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
#endif
            });

            var emailOptions = new EmailOptions
            {
                SenderName = configuration["Email:SenderName"] ?? "BOOKLY",
                SenderAddress = configuration["Email:SenderAddress"] ?? string.Empty,
                Smtp = new SmtpOptions
                {
                    Host = configuration["Email:Smtp:Host"] ?? string.Empty,
                    Port = int.TryParse(configuration["Email:Smtp:Port"], out var port) ? port : 587,
                    Username = configuration["Email:Smtp:Username"] ?? string.Empty,
                    Password = configuration["Email:Smtp:Password"] ?? string.Empty,
                    EnableSsl = !bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var enableSsl) || enableSsl
                }
            };

            var frontendOptions = new FrontendOptions
            {
                BaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173",
                ConfirmEmailPath = configuration["Frontend:ConfirmEmailPath"] ?? "/auth/confirm-email",
                ResetPasswordPath = configuration["Frontend:ResetPasswordPath"] ?? "/auth/reset-password",
                CompleteSecretaryInvitationPath = configuration["Frontend:CompleteSecretaryInvitationPath"] ?? "/auth/secretary-invitation"
            };

            services.AddSingleton(Options.Create(emailOptions));
            services.AddSingleton(Options.Create(frontendOptions));

            services.AddScoped<IServiceRepository, ServiceRepository>();

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BooklyDbContext>());

            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
            services.AddScoped<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<IInvitationTokenGenerator, InvitationTokenGenerator>();
            services.AddScoped<IUserTokenRepository, UserInvitationRepository>();
            services.AddScoped<IEmailService, SmtpEmailService>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IServiceTypeRepository, ServiceTypeRepository>();

            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
            services.AddScoped<IAppointmentHistoryRepository, AppointmentHistoryRepository>();

            return services;
        }
    }
}
