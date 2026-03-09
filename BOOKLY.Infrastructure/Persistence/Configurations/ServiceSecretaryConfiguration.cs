using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public class ServiceSecretaryConfiguration : IEntityTypeConfiguration<ServiceSecretary>
    {
        public void Configure(EntityTypeBuilder<ServiceSecretary> builder)
        {
            builder.ToTable("service_secretaries");

            builder.HasKey(x => new { x.ServiceId, x.SecretaryId });

            builder.Property(x => x.ServiceId)
                   .HasColumnName("service_id");

            builder.Property(x => x.SecretaryId)
                   .HasColumnName("secretary_id");

            // FK a User (secretary) - restrict (no cascada)
            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(x => x.SecretaryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.SecretaryId)
                   .HasDatabaseName("ix_service_secretaries_secretary_id");

            builder.HasIndex(x => x.ServiceId)
                   .HasDatabaseName("ix_service_secretaries_service_id");
        }
    }
}
