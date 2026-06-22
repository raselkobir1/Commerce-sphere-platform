using CommerceSphere.InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.InventoryService.Infrastructure.Data.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(i => i.Sku).HasColumnName("sku").HasMaxLength(100).IsRequired();
        builder.Property(i => i.QuantityOnHand).HasColumnName("quantity_on_hand").IsRequired();
        builder.Property(i => i.QuantityReserved).HasColumnName("quantity_reserved").IsRequired();
        builder.Property(i => i.ReorderLevel).HasColumnName("reorder_level").IsRequired();
        builder.Property(i => i.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        // Optimistic concurrency via xmin (PostgreSQL system column)
        builder.UseXminAsConcurrencyToken();

        // Unique index on product_id and sku
        builder.HasIndex(i => i.ProductId).IsUnique().HasDatabaseName("ix_inventory_items_product_id");
        builder.HasIndex(i => new { i.ProductId, i.Sku }).IsUnique().HasDatabaseName("ix_inventory_items_product_sku");
        builder.HasIndex(i => i.Sku).HasDatabaseName("ix_inventory_items_sku");
    }
}
