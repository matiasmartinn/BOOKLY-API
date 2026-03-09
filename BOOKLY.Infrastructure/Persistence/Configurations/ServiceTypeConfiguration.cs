// BOOKLY.Infrastructure/Persistence/Configurations/ServiceTypeConfiguration.cs
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public sealed class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
    {
        public void Configure(EntityTypeBuilder<ServiceType> builder)
        {
            builder.ToTable("service_types");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnName("service_type_id")
                   .ValueGeneratedOnAdd();

            builder.Property(x => x.Name)
                   .HasColumnName("name")
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Description)
                   .HasColumnName("description")
                   .HasMaxLength(500);

            builder.Property(x => x.IsActive)
                   .HasColumnName("is_active")
                   .IsRequired()
                   .HasDefaultValue(true);

            // Índice único para el nombre
            builder.HasIndex(x => x.Name)
                   .HasDatabaseName("ix_service_types_name")
                   .IsUnique();

            // FieldDefinitions (backing field)
            builder.Navigation(x => x.FieldDefinitions)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // Seed inicial (opcional)
            builder.HasData(
                new { Id = 1, Name = "Consulta", Description = "Consulta médica", IsActive = true },
                new { Id = 2, Name = "Tratamiento", Description = "Sesión de tratamiento", IsActive = true },
                new { Id = 3, Name = "Seguimiento", Description = "Consulta de seguimiento", IsActive = true }
            );
        }
    }
}