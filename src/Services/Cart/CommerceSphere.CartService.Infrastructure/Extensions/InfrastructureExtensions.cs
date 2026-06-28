using CommerceSphere.CartService.Application.Interfaces;
using CommerceSphere.CartService.Domain.Interfaces;
using CommerceSphere.CartService.Infrastructure.Data;
using CommerceSphere.CartService.Infrastructure.Kafka;
using CommerceSphere.CartService.Infrastructure.Kafka.Consumers;
using CommerceSphere.CartService.Infrastructure.Kafka.Producers;
using CommerceSphere.CartService.Infrastructure.Redis;
using CommerceSphere.Shared.Common.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CommerceSphere.CartService.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<CartDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("CartDb"),
                npg => npg.EnableRetryOnFailure(3)));

        var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<ICartCacheService, CartCacheService>();
        services.AddScoped<IIdempotencyService, RedisIdempotencyService>();

        services.AddSingleton<ICartEventProducer, CartEventProducer>();

        services.AddHostedService<InventorySagaConsumer>();
        // Publishes transactional-outbox rows to Kafka (reliable, never-lost event delivery).
        services.AddHostedService<OutboxRelay>();

        return services;
    }

    public static async Task MigrateCartDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        await db.Database.MigrateAsync();
    }
}
