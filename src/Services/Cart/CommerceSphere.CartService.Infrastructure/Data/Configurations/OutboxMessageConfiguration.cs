using CommerceSphere.CartService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.CartService.Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(m => m.Topic).HasColumnName("topic").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Key).HasColumnName("key").HasMaxLength(200).IsRequired();
        builder.Property(m => m.Payload).HasColumnName("payload").IsRequired();
        builder.Property(m => m.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        builder.Property(m => m.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.ProcessedAt).HasColumnName("processed_at");
        builder.Property(m => m.Attempts).HasColumnName("attempts");

        // Index for the relay's "find pending" query.
        builder.HasIndex(m => m.ProcessedAt).HasDatabaseName("ix_outbox_processed_at");
    }
}
