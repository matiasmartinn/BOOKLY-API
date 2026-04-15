using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BOOKLY.Application.Common;
using BOOKLY.Application.Interfaces;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.Repositories;
using BOOKLY.Infrastructure.Email;
using BOOKLY.Infrastructure.Persistence;
using BOOKLY.Infrastructure.Persistence.Repositories;
using BOOKLY.Infrastructure.Repositories;
using BOOKLY.Infrastructure.Security;
using BOOKLY.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BOOKLY.Infrastructure
{
    public static class InfraDependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("BooklyDb");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("ConnectionStrings:BooklyDb es requerida.");

            var normalizedConnectionString = SqlServerConnectionStringNormalizer.Normalize(connectionString);

            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings es requerida.");
            jwtSettings.Validate();


            var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()
                ?? throw new InvalidOperationException("Email es requerida.");

            var frontendOptions = configuration.GetSection("Frontend").Get<FrontendOptions>()
                ?? throw new InvalidOperationException("Frontend es requerida.");

            services.AddSingleton(Options.Create(emailOptions));
            services.AddSingleton(Options.Create(frontendOptions));
            services.AddSingleton(Options.Create(jwtSettings));

            services.AddDbContext<BooklyDbContext>(options =>
            {
                options.UseSqlServer(normalizedConnectionString);

#if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
#endif
            });


            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,
                        ClockSkew = TimeSpan.Zero,
                        NameClaimType = JwtRegisteredClaimNames.Email,
                        RoleClaimType = ClaimTypes.Role
                    };
                });

            services.AddAuthorization();

            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<IAdminRepository, AdminRepository>();

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BooklyDbContext>());

            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
            services.AddScoped<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<IInvitationTokenGenerator, InvitationTokenGenerator>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IUserTokenRepository, UserInvitationRepository>();
            services.AddScoped<IEmailService, SmtpEmailService>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IAppointmentHistoryRepository, AppointmentHistoryRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IServiceTypeRepository, ServiceTypeRepository>();

            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            return services;
        }
    }
}
