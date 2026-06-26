using CommerceSphere.AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.AuthService.Infrastructure.Data.Configurations;

public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("external_logins");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(50).IsRequired();
        builder.Property(e => e.ExternalUserId).HasColumnName("external_user_id").HasMaxLength(256).IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        // Composite unique constraint: one entry per (provider, externalUserId) combination
        // so the same social account can never be linked to two different local users.
        builder.HasIndex(e => new { e.Provider, e.ExternalUserId })
            .IsUnique()
            .HasDatabaseName("ix_external_logins_provider_external_user_id");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_external_logins_user_id");

        builder.HasOne(e => e.User)
            .WithMany(u => u.ExternalLogins)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
