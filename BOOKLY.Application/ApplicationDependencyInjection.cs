using BOOKLY.Application.Interfaces;
using BOOKLY.Application.EventHandler;
using BOOKLY.Application.Services;
using BOOKLY.Application.Services.AdminAggregate;
using BOOKLY.Application.Services.AppointmentAggregate;
using BOOKLY.Application.Services.ClientAggregate;
using BOOKLY.Application.Services.MetricsAggregate;
using BOOKLY.Application.Services.PublicBooking;
using BOOKLY.Application.Services.ServiceAggregate;
using BOOKLY.Application.Services.ServiceTypeAggregate;
using BOOKLY.Application.Services.SubscriptionAggregate;
using BOOKLY.Application.Services.UserAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Events;
using BOOKLY.Domain.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace BOOKLY.Application.DependencyInjection
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // AutoMapper
            services.AddAutoMapper(typeof(ApplicationServiceExtensions).Assembly);

            // SERVICE AGGREGATES
            services.AddScoped<IServiceApplicationService, ServiceApplicationService>();
            services.AddScoped<IPublicBookingService, PublicBookingService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IMetricsService, MetricsService>();
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            services.AddScoped<IAdminOwnersService, AdminOwnersService>();
            services.AddScoped<IAdminServicesService, AdminServicesService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<IServiceTypeService, ServiceTypeService>();
            services.AddScoped<IServiceAuthorizationService, ServiceAuthorizationService>();
            services.AddScoped<IDomainEventHandler<AppointmentCreatedEvent>, RecordAppointmentCreatedHandler>();
            services.AddScoped<IDomainEventHandler<AppointmentStatusChangedEvent>, RecordStatusChangedHandler>();

            // SECURITY
            services.AddScoped<ITokenHashingService, TokenHashingService>();

            return services;
        }
    }
}
