using CommerceSphere.CartService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.CartService.Infrastructure.Data.Configurations;

public class OrderStatusEntryConfiguration : IEntityTypeConfiguration<OrderStatusEntry>
{
    public void Configure(EntityTypeBuilder<OrderStatusEntry> builder)
    {
        builder.ToTable("order_status_history");
        builder.HasKey(e => e.Id);

        // Domain-generated key (same rationale as CartItem).
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.CartId).HasColumnName("cart_id").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Note).HasColumnName("note").HasMaxLength(300);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(e => e.CartId).HasDatabaseName("ix_order_status_history_cart_id");
    }
}
