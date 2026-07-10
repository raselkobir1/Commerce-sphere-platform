using CommerceSphere.ChatService.Domain.Entities;
using CommerceSphere.ChatService.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.ChatService.Infrastructure.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ConversationConfiguration());
        modelBuilder.ApplyConfiguration(new ChatMessageConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
