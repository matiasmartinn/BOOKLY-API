using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
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

            builder.Property(x => x.Description)
                   .HasColumnName("description")
                   .HasMaxLength(500);

            builder.Property(x => x.PhoneNumber)
                   .HasColumnName("phone_number")
                   .HasMaxLength(50);

            builder.OwnsOne(x => x.Location, location =>
            {
                location.Property(x => x.PlaceName)
                        .HasColumnName("place_name")
                        .HasMaxLength(150);

                location.Property(x => x.Address)
                        .HasColumnName("address")
                        .HasMaxLength(250);

                location.Property(x => x.GoogleMapsUrl)
                        .HasColumnName("google_maps_url")
                        .HasMaxLength(500);

                location.WithOwner();
            });

            builder.Property(x => x.IsActive)
                   .HasColumnName("is_active")
                   .HasDefaultValue(true);

            builder.Property(x => x.IsPublicBookingEnabled)
                   .HasColumnName("is_public_booking_enabled")
                   .HasDefaultValue(true)
                   .IsRequired();

            builder.Property(x => x.PublicBookingCode)
                   .HasColumnName("public_booking_code")
                   .HasMaxLength(8)
                   .IsRequired();

            builder.Property(x => x.PublicBookingCodeUpdatedAt)
                   .HasColumnName("public_booking_code_updated_at")
                   .HasColumnType("datetime2");

            builder.Property(x => x.Price)
                   .HasColumnName("price")
                   .HasPrecision(18, 2);

            builder.Property(x => x.ServiceTypeId)
                   .HasColumnName("service_type_id")
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .HasColumnName("created_at")
                   .HasColumnType("datetime2")
                   .HasDefaultValueSql("GETDATE()")
                   .IsRequired();

            builder.Property(x => x.Mode)
                   .HasColumnName("mode")
                   .HasConversion<int>();

            builder.HasIndex(x => x.OwnerId)
                   .HasDatabaseName("ix_services_owner_id");

            builder.HasIndex(x => x.PublicBookingCode)
                   .IsUnique()
                   .HasDatabaseName("ux_services_public_booking_code");

            builder.HasIndex(x => x.ServiceTypeId)
                   .HasDatabaseName("ix_services_service_type_id");

            builder.HasIndex(x => x.CreatedAt)
                   .HasDatabaseName("ix_services_created_at");

            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(x => x.OwnerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ServiceType>()
                   .WithMany()
                   .HasForeignKey(x => x.ServiceTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

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

            builder.OwnsOne(x => x.DurationMinutes, duration =>
            {
                duration.Property(d => d.Value)
                        .HasColumnName("duration_minutes")
                        .IsRequired();
            });

            builder.OwnsOne(x => x.Capacity, capacity =>
            {
                capacity.Property(c => c.Value)
                        .HasColumnName("capacity")
                        .IsRequired();
            });

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
                         .HasColumnType("time")
                         .IsRequired();

                    range.Property(r => r.End)
                         .HasColumnName("end_time")
                         .HasColumnType("time")
                         .IsRequired();
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

            builder.Navigation(x => x.ServicesUnavailability)
                   .HasField("_serviceUnavailability")
                   .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.OwnsMany(x => x.ServicesUnavailability, unavail =>
            {
                unavail.ToTable("service_unavailabilities");
                unavail.WithOwner().HasForeignKey("service_id");

                unavail.HasKey(u => u.Id);

                unavail.Property(u => u.Id)
                       .HasColumnName("service_unavailability_id")
                       .ValueGeneratedOnAdd();

                unavail.OwnsOne(u => u.DateRange, dateRange =>
                {
                    dateRange.Property(d => d.Start)
                             .HasColumnName("start_date")
                             .HasColumnType("date")
                             .IsRequired();

                    dateRange.Property(d => d.End)
                             .HasColumnName("end_date")
                             .HasColumnType("date")
                             .IsRequired();
                });

                unavail.OwnsOne(u => u.TimeRange, timeRange =>
                {
                    timeRange.Property(t => t.Start)
                             .HasColumnName("start_time")
                             .HasColumnType("time");

                    timeRange.Property(t => t.End)
                             .HasColumnName("end_time")
                             .HasColumnType("time");
                });

                unavail.Property(u => u.Reason)
                       .HasColumnName("reason")
                       .HasMaxLength(250);

                unavail.HasIndex("service_id")
                       .HasDatabaseName("ix_service_unavailabilities_service_id");
            });

            builder.HasMany(s => s.ServiceSecretaries)
                   .WithOne()
                   .HasForeignKey(ss => ss.ServiceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(s => s.ServiceSecretaries)
                   .HasField("_serviceSecretaries")
                   .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Ignore(x => x.SecretaryIds);
        }
    }
}
