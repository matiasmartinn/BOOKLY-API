using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("appointments");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(a => a.ServiceId).HasColumnName("service_id").IsRequired();
            builder.Property(a => a.AssignedSecretaryId).HasColumnName("assigned_secretary_id");
            builder.Property(a => a.StartDateTime).HasColumnName("start_date_time").IsRequired();
            builder.Property(a => a.EndDateTime)
                .HasColumnName("end_date_time")
                .IsRequired();
            builder.Property(a => a.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            builder.Property(a => a.ClientNotes).HasColumnName("client_notes").HasMaxLength(1000);
            builder.Property(a => a.CancelReason).HasColumnName("cancel_reason").HasMaxLength(1000);
            builder.Property(a => a.CancelledOn).HasColumnName("cancelled_on");
            builder.Property(a => a.CreatedOn).HasColumnName("created_on").IsRequired();
            builder.Property(a => a.UpdateOn).HasColumnName("update_on");
            builder.Property(a => a.UpdateBy).HasColumnName("update_by");

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
