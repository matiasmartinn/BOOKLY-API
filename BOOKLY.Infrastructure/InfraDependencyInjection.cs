using BOOKLY.Domain.Interfaces;
using BOOKLY.Infrastructure.Persistence;
using BOOKLY.Infrastructure.Persistence.Repositories;
using BOOKLY.Infrastructure.Repositories;
using BOOKLY.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

            services.AddScoped<IServiceRepository, ServiceRepository>();

            // Unit of Work en el mismo Scoped que BooklyDbContext.
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BooklyDbContext>());

            // UTILITIES
            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
            services.AddScoped<IInvitationTokenGenerator, InvitationTokenGenerator>();
            services.AddScoped<IUserInvitationRepository,UserInvitationRepository >();

            // AGGREGATES 
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IServiceTypeRepository, ServiceTypeRepository>(); 
            
            // EVENTS
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
            services.AddScoped<IAppointmentHistoryRepository, AppointmentHistoryRepository>();

            return services;
            }
        }
    }