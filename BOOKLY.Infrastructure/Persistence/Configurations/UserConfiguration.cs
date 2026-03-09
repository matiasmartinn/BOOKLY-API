using BOOKLY.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnName("user_id")
                   .ValueGeneratedOnAdd();

            builder.OwnsOne(x => x.PersonName, personName =>
            {
                personName.Property(p => p.FirstName)
                   .HasColumnName("first_name")
                   .HasMaxLength(100)
                   .IsRequired();

                personName.Property(p => p.LastName)
                   .HasColumnName("last_name")
                   .HasMaxLength(100)
                   .IsRequired();
            });

            builder.OwnsOne(x => x.Password, Password =>
            {
                Password.Property(p => p.Hash)
                   .HasColumnName("password_hash")
                   .HasMaxLength(128)
                   .IsRequired(false);
            });

            // SETEAR LA NAVEGACION A PASSWORD COMO NULL SI ROMPE. OWNSONE.

            builder.Property(x => x.Role)
                   .HasColumnName("role")
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(x => x.IsActive)
                   .HasColumnName("is_active")
                   .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("GETDATE()")
                   .IsRequired();

            builder.Property(x => x.LastLoginAt)
                   .HasColumnName("last_login_at");

            builder.OwnsOne(x => x.Email, email =>
            {
                email.Property(e => e.Value)
                     .HasColumnName("email")
                     .HasMaxLength(255)
                     .IsRequired();

                email.HasIndex(e => e.Value)
                     .HasDatabaseName("ix_users_email")
                     .IsUnique();
            });

            builder.HasIndex(x => x.Role)
                   .HasDatabaseName("ix_users_role");

            builder.HasIndex(x => x.IsActive)
                   .HasDatabaseName("ix_users_is_active");

            builder.HasIndex(x => x.CreatedAt)
                   .HasDatabaseName("ix_users_created_at");
        }
    }
}

