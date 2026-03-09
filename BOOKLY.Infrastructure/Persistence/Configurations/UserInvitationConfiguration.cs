using BOOKLY.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace BOOKLY.Infrastructure.Persistence.Configurations
{
    public sealed class UserInvitationConfiguration : IEntityTypeConfiguration<UserInvitation>
    {
        public void Configure(EntityTypeBuilder<UserInvitation> builder)
        {
            builder.ToTable("user_invitations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnName("user_invitation_id")
                   .ValueGeneratedOnAdd();

            builder.Property(x => x.UserId)
                   .HasColumnName("user_id")
                   .IsRequired();

            builder.Property(x => x.TokenHash)
                   .HasColumnName("token_hash")
                   .HasMaxLength(64) // SHA-256 hex = 64 chars
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
                   .HasDatabaseName("ix_user_invitations_token_hash")
                   .IsUnique();

            builder.HasIndex(x => x.UserId)
                   .HasDatabaseName("ix_user_invitations_user_id");

            builder.HasIndex(x => x.ExpiresOn)
                   .HasDatabaseName("ix_user_invitations_expires_on");

            builder.HasIndex(x => x.UsedOn)
                   .HasDatabaseName("ix_user_invitations_used_on");
        }
    }
}