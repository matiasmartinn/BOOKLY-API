using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("appointments");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .HasColumnName("appointment_id")
                .ValueGeneratedOnAdd();

            builder.Property(a => a.ServiceId)
                .HasColumnName("service_id")
                .IsRequired();

            builder.Property(a => a.AssignedSecretaryId)
                .HasColumnName("assigned_secretary_id");

            builder.Property(a => a.StartDateTime)
                .HasColumnName("start_date_time")
                .IsRequired();

            builder.Property(a => a.EndDateTime)
                .HasColumnName("end_date_time")
                .IsRequired();

            builder.Property(a => a.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(a => a.ClientNotes)
                .HasColumnName("client_notes")
                .HasMaxLength(1000);

            builder.Property(a => a.CancelReason)
                .HasColumnName("cancel_reason")
                .HasMaxLength(1000);

            builder.Property(a => a.CancelledOn)
                .HasColumnName("cancelled_on");

            builder.Property(a => a.CreatedOn)
                .HasColumnName("created_on")
                .IsRequired();

            builder.Property(a => a.UpdateOn)
                .HasColumnName("updated_on");

            builder.Property(a => a.UpdateBy)
                .HasColumnName("updated_by");

            builder.HasIndex(a => a.ServiceId)
                .HasDatabaseName("ix_appointments_service_id");

            builder.HasIndex(a => a.AssignedSecretaryId)
                .HasDatabaseName("ix_appointments_assigned_secretary_id");

            builder.HasIndex(a => a.UpdateBy)
                .HasDatabaseName("ix_appointments_updated_by");

            builder.HasOne<Service>()
                .WithMany()
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.AssignedSecretaryId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UpdateBy)
                .OnDelete(DeleteBehavior.NoAction);

            builder.OwnsOne(a => a.Duration, duration =>
            {
                duration.Property(d => d.Value)
                    .HasColumnName("duration_minutes")
                    .IsRequired();
            });

            builder.OwnsOne(a => a.Client, client =>
            {
                client.Property(c => c.ClientName)
                    .HasColumnName("client_name")
                    .HasMaxLength(200)
                    .IsRequired();

                client.Property(c => c.Phone)
                    .HasColumnName("client_phone")
                    .HasMaxLength(50)
                    .IsRequired();

                client.OwnsOne(c => c.Email, email =>
                {
                    email.Property(e => e.Value)
                        .HasColumnName("client_email")
                        .HasMaxLength(255);
                });
            });

            builder.Navigation(a => a.FieldValues)
                   .HasField("_fieldValues")
                   .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(a => a.FieldValues)
                   .WithOne()
                   .HasForeignKey(fv => fv.AppointmentId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
