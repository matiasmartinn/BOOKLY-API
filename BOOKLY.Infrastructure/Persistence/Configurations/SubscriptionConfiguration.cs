using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.ToTable("subscriptions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(s => s.OwnerId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.HasIndex(s => s.OwnerId)
                .HasDatabaseName("ux_subscriptions_user_id")
                .IsUnique();

            builder.Property(s => s.Status)
                .HasColumnName("status")
                .HasConversion<int>()
                .IsRequired();

            builder.OwnsOne(s => s.Period, period =>
            {
                period.Property(p => p.StartDate)
                    .HasColumnName("start_date")
                    .HasConversion(
                        v => v.ToDateTime(TimeOnly.MinValue),
                        v => DateOnly.FromDateTime(v))
                    .HasColumnType("date")
                    .IsRequired();

                period.Property(p => p.EndDate)
                    .HasColumnName("end_date")
                    .HasConversion(
                        v => v.HasValue ? v.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                        v => v.HasValue ? DateOnly.FromDateTime(v.Value) : (DateOnly?)null)
                    .HasColumnType("date");
            });

            builder.OwnsOne(s => s.Plan, plan =>
            {
                plan.Property(p => p.Name)
                    .HasColumnName("plan_name")
                    .HasConversion<int>()
                    .IsRequired();

                plan.Property(p => p.MaxServices)
                    .HasColumnName("max_services")
                    .IsRequired();

                plan.Property(p => p.MaxSecretaries)
                    .HasColumnName("max_secretaries")
                    .IsRequired();
            });

            builder.Navigation(s => s.Period).IsRequired();
            builder.Navigation(s => s.Plan).IsRequired();

            builder.Property(s => s.CreatedOn)
                .HasColumnName("created_on")
                .HasColumnType("datetime2")
                .IsRequired();
            // Opcional: si querés que DB lo seteé
            // .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(s => s.UpdatedOn)
                .HasColumnName("updated_on")
                .HasColumnType("datetime2");

            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "ck_subscriptions_period_dates",
                    "[end_date] IS NULL OR [end_date] >= [start_date]"
                );
            });

            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "ck_subscriptions_free_end_date",
                    "[plan_name] <> 1 OR [end_date] IS NULL"
                );
            });
        }
    }
}
