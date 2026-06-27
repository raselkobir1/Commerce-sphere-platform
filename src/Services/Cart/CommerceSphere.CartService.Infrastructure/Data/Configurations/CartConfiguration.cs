using CommerceSphere.CartService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.CartService.Infrastructure.Data.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts");

        builder.HasKey(c => c.Id);

        // Domain-generated key (see CartItemConfiguration for the full rationale).
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(256);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("ix_carts_user_id");

        builder.HasIndex(c => c.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ix_carts_idempotency_key")
            .HasFilter("idempotency_key IS NOT NULL");

        builder.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
