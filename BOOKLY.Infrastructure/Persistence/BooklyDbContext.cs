using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Entities;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Persistence
{
    public sealed class BooklyDbContext : DbContext, IUnitOfWork
    {
        private readonly IDomainEventDispatcher _dispatcher;
        public BooklyDbContext(DbContextOptions<BooklyDbContext> options, IDomainEventDispatcher domainEventDispatcher) : base(options) {
        _dispatcher = domainEventDispatcher;
        }

        public DbSet<Service> Services => Set<Service>();
        public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
        public DbSet<ServiceSecretary> ServiceSecretaries => Set<ServiceSecretary>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<AppointmentFieldValue> AppointmentFieldsValues => Set<AppointmentFieldValue>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<ServiceTypeFieldDefinition> ServiceTypeFieldDefinitions => Set<ServiceTypeFieldDefinition>();
        public DbSet<ServiceTypeFieldOption> ServiceTypeFieldOptions => Set<ServiceTypeFieldOption>();
        public DbSet<AppointmentStatusHistory> AppointmentStatusHistories => Set<AppointmentStatusHistory>();

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            //  Capturar aggregates ANTES de guardar (después el ChangeTracker puede no tenerlos)
            var aggregates = ChangeTracker
                .Entries<AggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            int result;

            try
            {
                result = await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
            {
                throw sqlEx.Number switch
                {
                    2627 or 2601 => new ConflictException("Ya existe un registro con esos datos únicos."),
                    547 => new ConflictException("Violación de integridad referencial."),
                    _ => new ConflictException($"Error de base de datos ({sqlEx.Number}).")
                };
            }

            foreach (var aggregate in aggregates)
            {
                await _dispatcher.Dispatch(aggregate.DomainEvents, cancellationToken);
                aggregate.ClearDomainEvents();
            }

            return result;
        }
        public Task<int> SaveChanges(CancellationToken cancellationToken = default)
            => SaveChangesAsync(cancellationToken);
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BooklyDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}