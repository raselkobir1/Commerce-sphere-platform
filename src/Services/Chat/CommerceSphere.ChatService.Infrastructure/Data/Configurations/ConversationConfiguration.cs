using CommerceSphere.ChatService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.ChatService.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(c => c.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.CustomerEmail).HasColumnName("customer_email").HasMaxLength(256);
        builder.Property(c => c.LastMessagePreview).HasColumnName("last_message_preview").HasMaxLength(200);
        builder.Property(c => c.LastMessageAt).HasColumnName("last_message_at").IsRequired();
        builder.Property(c => c.UnreadForSupport).HasColumnName("unread_for_support");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        // One support conversation per customer — enforced with a unique index.
        builder.HasIndex(c => c.CustomerId).IsUnique().HasDatabaseName("ix_conversations_customer_id");
        builder.HasIndex(c => c.LastMessageAt).HasDatabaseName("ix_conversations_last_message_at");
    }
}
