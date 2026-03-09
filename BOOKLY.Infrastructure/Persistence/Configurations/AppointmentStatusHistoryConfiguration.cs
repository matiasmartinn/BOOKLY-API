using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public sealed class AppointmentStatusHistoryConfiguration : IEntityTypeConfiguration<AppointmentStatusHistory>
    {
        public void Configure(EntityTypeBuilder<AppointmentStatusHistory> builder)
        {
            builder.ToTable("appointment_status_history");
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OldStatus).IsRequired(false).HasColumnName("old_status");
            builder.Property(x => x.NewStatus).IsRequired().HasColumnName("new_status");
            builder.Property(x => x.Reason).IsRequired(false).HasColumnName("reason");
            builder.Property(x => x.OccurredOn).IsRequired().HasColumnName("occurred_on");
            builder.Property(x => x.AppointmentId).IsRequired().HasColumnName("appointment_id");

            builder.HasOne<Appointment>()
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
