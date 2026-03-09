using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public sealed class ServiceTypeFieldOptionConfiguration : IEntityTypeConfiguration<ServiceTypeFieldOption>
    {
        public void Configure(EntityTypeBuilder<ServiceTypeFieldOption> builder)
        {
            builder.ToTable("service_type_field_options");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("field_option_id")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.FieldDefinitionId)
                .HasColumnName("field_definition_id")
                .IsRequired();

            builder.Property(x => x.Value)
                .HasColumnName("value")
                .HasMaxLength(60)
                .IsRequired();

            builder.Property(x => x.Label)
                .HasColumnName("label")
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(x => x.SortOrder)
                .HasColumnName("sort_order")
                .IsRequired();

            builder.Property(x => x.IsActive)
                .HasColumnName("is_active")
                .IsRequired()
                .HasDefaultValue(true);

            // Relationship: Definition 1 - N Options
            builder.HasOne<ServiceTypeFieldDefinition>()
                .WithMany(d => d.Options)
                .HasForeignKey(x => x.FieldDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique: (field_definition_id, value)
            builder.HasIndex(x => new { x.FieldDefinitionId, x.Value })
                .HasDatabaseName("ux_service_type_field_options_definition_value")
                .IsUnique();

            builder.HasIndex(x => new { x.FieldDefinitionId, x.SortOrder })
                .HasDatabaseName("ix_service_type_field_options_definition_sort");
        }
    }
}