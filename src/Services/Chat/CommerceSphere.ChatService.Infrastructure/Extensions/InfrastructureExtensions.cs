using CommerceSphere.ChatService.Application.Interfaces;
using CommerceSphere.ChatService.Application.Managers;
using CommerceSphere.ChatService.Domain.Interfaces;
using CommerceSphere.ChatService.Domain.Interfaces.Repositories;
using CommerceSphere.ChatService.Infrastructure.Data;
using CommerceSphere.ChatService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceSphere.ChatService.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ChatDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("ChatDb"),
                npg => npg.EnableRetryOnFailure(3)));

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<IChatManager, ChatManager>();

        return services;
    }

    public static async Task MigrateChatDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        await db.Database.MigrateAsync();
    }
}
