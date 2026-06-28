using CommerceSphere.NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceSphere.NotificationService.Infrastructure.Data.Configurations;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(m => m.Key);

        builder.Property(m => m.Key).HasColumnName("key").HasMaxLength(200);
        builder.Property(m => m.ProcessedAt).HasColumnName("processed_at").IsRequired();
    }
}
