using CommerceSphere.ProductService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.ProductService.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(2000).IsRequired();
        builder.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Price).HasColumnName("price").HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
        builder.Property(p => p.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
        builder.Property(p => p.IsActive).HasColumnName("is_active");
        builder.Property(p => p.Stock).HasColumnName("stock");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        // Optimistic concurrency via the PostgreSQL xmin system column. BaseEntity exposes an
        // explicit `uint RowVersion` property, so map it to xmin directly. Using
        // UseXminAsConcurrencyToken() instead leaves RowVersion mapped to a (non-existent)
        // "RowVersion" column, so every query fails with 42703 — same bug fixed in AuthService.
        builder.Property(p => p.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("ix_products_sku");
        builder.HasIndex(p => p.Category).HasDatabaseName("ix_products_category");
    }
}
