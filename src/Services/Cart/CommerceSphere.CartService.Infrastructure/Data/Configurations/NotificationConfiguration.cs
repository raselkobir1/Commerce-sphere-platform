using CommerceSphere.CartService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.CartService.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);

        // Domain-generated key (same rationale as Cart).
        builder.Property(n => n.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(n => n.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
        builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
        builder.Property(n => n.Message).HasColumnName("message").HasMaxLength(500).IsRequired();
        builder.Property(n => n.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(n => n.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(n => n.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
        builder.Property(n => n.ItemCount).HasColumnName("item_count");
        builder.Property(n => n.IsRead).HasColumnName("is_read");
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(n => n.IsRead).HasDatabaseName("ix_notifications_is_read");
        builder.HasIndex(n => n.CreatedAt).HasDatabaseName("ix_notifications_created_at");
    }
}
