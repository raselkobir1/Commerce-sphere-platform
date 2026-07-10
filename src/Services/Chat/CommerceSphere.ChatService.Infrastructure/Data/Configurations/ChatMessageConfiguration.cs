using CommerceSphere.ChatService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.ChatService.Infrastructure.Data.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(m => m.ConversationId).HasColumnName("conversation_id").IsRequired();
        builder.Property(m => m.SenderId).HasColumnName("sender_id").IsRequired();
        builder.Property(m => m.SenderRole).HasColumnName("sender_role").HasMaxLength(20).IsRequired();
        builder.Property(m => m.SenderName).HasColumnName("sender_name").HasMaxLength(200).IsRequired();
        builder.Property(m => m.Content).HasColumnName("content").HasMaxLength(2000).IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");

        // History is always fetched per-conversation, oldest first.
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt })
            .HasDatabaseName("ix_chat_messages_conversation_created");
    }
}
