using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public sealed class ServiceTypeFieldDefinitionConfiguration : IEntityTypeConfiguration<ServiceTypeFieldDefinition>
    {
        public void Configure(EntityTypeBuilder<ServiceTypeFieldDefinition> builder)
        {
            builder.ToTable("service_type_field_definitions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("field_definition_id")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.ServiceTypeId)
                .HasColumnName("service_type_id")
                .IsRequired();

            // Value Objects: Key, Label
            builder.OwnsOne(x => x.Key, fk =>
            {
                fk.Property(p => p.Value)
                  .HasColumnName("key")
                  .HasMaxLength(60)
                  .IsRequired();

            });

            builder.OwnsOne(x => x.Label, fl =>
            {
                fl.Property(p => p.Value)
                  .HasColumnName("label")
                  .HasMaxLength(80)
                  .IsRequired();
            });

            builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            builder.Property(x => x.FieldType)
                .HasColumnName("field_type")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.IsRequired)
                .HasColumnName("is_required")
                .IsRequired();

            builder.Property(x => x.IsActive)
                .HasColumnName("is_active")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.SortOrder)
                .HasColumnName("sort_order")
                .IsRequired();

            builder.Property(x => x.CreatedOn)
                .HasColumnName("created_on")
                .IsRequired();

            builder.Property(x => x.UpdatedOn)
                .HasColumnName("updated_on");

            // Relationship: ServiceType 1 - N FieldDefinitions
            builder.HasOne<ServiceType>()
                .WithMany(st => st.FieldDefinitions)
                .HasForeignKey(x => x.ServiceTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index útil para ordenar por UIs
            builder.HasIndex(x => new { x.ServiceTypeId, x.SortOrder })
                .HasDatabaseName("ix_service_type_field_definitions_service_type_sort");
        }
    }
}