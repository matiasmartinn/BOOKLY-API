using BOOKLY.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public sealed class UserInvitationConfiguration : IEntityTypeConfiguration<UserToken>
    {
        public void Configure(EntityTypeBuilder<UserToken> builder)
        {
            builder.ToTable("user_tokens");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnName("user_token_id")
                   .ValueGeneratedOnAdd();

            builder.Property(x => x.UserId)
                   .HasColumnName("user_id")
                   .IsRequired();

            builder.Property(x => x.Purpose)
                   .HasColumnName("purpose")
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.TokenHash)
                   .HasColumnName("token_hash")
                   .HasMaxLength(64)
                   .IsRequired();

            builder.Property(x => x.CreatedOn)
                   .HasColumnName("created_on")
                   .IsRequired();

            builder.Property(x => x.ExpiresOn)
                   .HasColumnName("expires_on")
                   .IsRequired();

            builder.Property(x => x.UsedOn)
                   .HasColumnName("used_on");

            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.TokenHash)
                   .HasDatabaseName("ix_user_tokens_token_hash")
                   .IsUnique();

            builder.HasIndex(x => x.UserId)
                   .HasDatabaseName("ix_user_tokens_user_id");

            builder.HasIndex(x => new { x.UserId, x.Purpose })
                   .HasDatabaseName("ix_user_tokens_user_id_purpose");

            builder.HasIndex(x => x.ExpiresOn)
                   .HasDatabaseName("ix_user_tokens_expires_on");

            builder.HasIndex(x => x.UsedOn)
                   .HasDatabaseName("ix_user_tokens_used_on");
        }
    }
}
