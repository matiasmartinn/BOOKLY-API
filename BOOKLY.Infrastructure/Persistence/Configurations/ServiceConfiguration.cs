using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.ToTable("services");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnName("service_id")
                   .ValueGeneratedOnAdd();

            builder.Property(x => x.Name)
                   .HasColumnName("name")
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.OwnerId)
                   .HasColumnName("owner_id")
                   .IsRequired();

            builder.HasIndex(x => x.OwnerId)
                    .HasDatabaseName("ix_services_owner_id");

            builder.Property(x => x.Description)
                   .HasColumnName("description")
                   .HasMaxLength(500);

            builder.Property(x => x.IsActive)
                   .HasColumnName("is_active")
                   .HasDefaultValue(true);

            builder.Property(x => x.Price)
                   .HasColumnName("price")
                   .HasPrecision(18, 2);

            builder.Property(x => x.ServiceTypeId)
                   .HasColumnName("service_type_id")
                   .IsRequired();

            builder.HasIndex(x => x.ServiceTypeId)
                   .HasDatabaseName("ix_services_service_type_id");

            builder.Property(x => x.Mode)
                   .HasColumnName("mode")
                   .HasConversion<int>();

            // =========================
            // Slug (Value Object - Owned)
            // =========================
            builder.OwnsOne(x => x.Slug, slug =>
            {
                slug.Property(s => s.Value)
                    .HasColumnName("slug")
                    .HasMaxLength(150)
                    .IsRequired();

                slug.HasIndex(s => s.Value)
                    .HasDatabaseName("ix_services_slug")
                    .IsUnique();
            });

            // =========================
            // Duration (Value Object - Owned)
            // =========================
            builder.OwnsOne(x => x.DurationMinutes, duration =>
            {
                duration.Property(d => d.Value)
                        .HasColumnName("duration_minutes")
                        .IsRequired();
            });

            // =========================================================
            // ServiceSchedules (Owned Collection) 
            // =========================================================
            builder.Navigation(x => x.ServiceSchedules)
                   .HasField("_serviceSchedules")
                   .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.OwnsMany(x => x.ServiceSchedules, schedules =>
            {
                schedules.ToTable("service_schedules");
                schedules.WithOwner().HasForeignKey("service_id");

                schedules.HasKey(s => s.Id);

                schedules.Property(s => s.Id)
                         .HasColumnName("service_schedule_id")
                         .ValueGeneratedOnAdd();

                schedules.OwnsOne(s => s.Range, range =>
                {
                    range.Property(r => r.Start)
                         .HasColumnName("start_time")
                         .HasColumnType("time");

                    range.Property(r => r.End)
                         .HasColumnName("end_time")
                         .HasColumnType("time");
                });

                schedules.OwnsOne(s => s.Capacity, cap =>
                {
                    cap.Property(c => c.Value)
                       .HasColumnName("capacity")
                       .IsRequired();
                });

                schedules.OwnsOne(s => s.Day, day =>
                {
                    day.Property(d => d.Value)
                       .HasColumnName("day")
                       .IsRequired();
                });

                schedules.HasIndex("service_id")
                         .HasDatabaseName("ix_service_schedules_service_id");
            });

            // =========================================================
            // ServiceSchedulesUnavailability (Owned Collection) con backing field
            // =========================================================
            builder.Navigation(x => x.ServiceSchedulesUnavailability)
                   .HasField("_serviceSchedulesUnavailability")
                   .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.OwnsMany(x => x.ServiceSchedulesUnavailability, unavail =>
            {
                unavail.ToTable("service_schedule_unavailability");
                unavail.WithOwner().HasForeignKey("service_id");

                unavail.HasKey(u => u.Id);

                unavail.Property(u => u.Id)
                       .HasColumnName("service_schedule_unavailability_id")
                       .ValueGeneratedOnAdd();

                unavail.Property(u => u.Date)
                       .HasColumnName("date")
                       .HasColumnType("date")
                       .IsRequired();

                // Range puede ser null (día completo)
                unavail.OwnsOne(u => u.Range, range =>
                {
                    range.Property(r => r.Start)
                         .HasColumnName("start_time")
                         .HasColumnType("time");

                    range.Property(r => r.End)
                         .HasColumnName("end_time")
                         .HasColumnType("time");
                });

                unavail.Property(u => u.Reason)
                       .HasColumnName("reason")
                       .HasMaxLength(250);

                unavail.HasIndex("service_id")
                       .HasDatabaseName("ix_service_schedule_unavailability_service_id");

            });

            // =========================================================
            // ServiceSecretaries (Join Entity) - controlado por Service
            // ========================================================
            builder.Navigation(s => s.ServiceSecretaries)
                    .HasField("_serviceSecretaries")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(s => s.ServiceSecretaries)
                   .WithOne()
                   .HasForeignKey(ss => ss.ServiceId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Proyección: SecretaryIds no se persiste
            builder.Ignore(x => x.SecretaryIds);

            // =========================
            // Índices
            // =========================
            builder.HasIndex(x => x.OwnerId)
                   .HasDatabaseName("ix_services_owner_id");

            builder.HasIndex(x => x.ServiceTypeId)
                   .HasDatabaseName("ix_services_service_type_id");
        }
    }
}