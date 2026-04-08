using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public sealed class AppointmentFieldValueConfiguration : IEntityTypeConfiguration<AppointmentFieldValue>
    {
        public void Configure(EntityTypeBuilder<AppointmentFieldValue> builder)
        {
            builder.ToTable("appointment_field_values");
            builder.HasKey(fv => fv.Id);

            builder.Property(fv => fv.Id)
                .HasColumnName("appointment_field_value_id")
                .ValueGeneratedOnAdd();

            builder.Property(fv => fv.AppointmentId)
                .HasColumnName("appointment_id")
                .IsRequired();

            builder.Property(fv => fv.FieldDefinitionId)
                .HasColumnName("field_definition_id")
                .IsRequired();

            builder.Property(fv => fv.Value)
                .HasColumnName("value")
                .HasMaxLength(2000)
                .IsRequired();

            builder.HasOne<Appointment>()
                .WithMany(a => a.FieldValues)
                .HasForeignKey(fv => fv.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ServiceTypeFieldDefinition>()
                .WithMany()
                .HasForeignKey(fv => fv.FieldDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(fv => new { fv.AppointmentId, fv.FieldDefinitionId })
                .HasDatabaseName("ux_appointment_field_values_appointment_field")
                .IsUnique();
        }
    }
}
