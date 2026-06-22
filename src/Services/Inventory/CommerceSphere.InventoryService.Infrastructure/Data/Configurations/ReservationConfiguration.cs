using CommerceSphere.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.InventoryService.Infrastructure.Data.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.CartId).HasColumnName("cart_id").IsRequired();
        builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion<string>();
        builder.Property(r => r.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(256).IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        // Owned collection of reservation items
        builder.OwnsMany(r => r.Items, item =>
        {
            item.ToTable("reservation_items");
            item.WithOwner().HasForeignKey("reservation_id");
            item.Property<Guid>("Id").HasColumnName("id").ValueGeneratedOnAdd();
            item.HasKey("Id");
            item.Property(i => i.ProductId).HasColumnName("product_id").IsRequired();
            item.Property(i => i.Sku).HasColumnName("sku").HasMaxLength(100).IsRequired();
            item.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();
            item.Property(i => i.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2).IsRequired();
        });

        builder.HasIndex(r => r.CartId).HasDatabaseName("ix_reservations_cart_id");
        builder.HasIndex(r => r.IdempotencyKey).IsUnique().HasDatabaseName("ix_reservations_idempotency_key");
    }
}
