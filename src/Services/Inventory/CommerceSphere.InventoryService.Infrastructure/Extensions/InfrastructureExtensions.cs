using CommerceSphere.InventoryService.Application.Interfaces;
using CommerceSphere.InventoryService.Domain.Interfaces;
using CommerceSphere.InventoryService.Infrastructure.Data;
using CommerceSphere.InventoryService.Infrastructure.Kafka.Consumers;
using CommerceSphere.InventoryService.Infrastructure.Kafka.Producers;
using CommerceSphere.InventoryService.Infrastructure.Redis;
using CommerceSphere.Shared.Common.Idempotency;
using CommerceSphere.Shared.Common.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CommerceSphere.InventoryService.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Npgsql / EF Core
        services.AddDbContext<InventoryDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("InventoryDb"),
                npg => npg.EnableRetryOnFailure(3)));

        // Redis
        var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

        // Unit of Work (scoped)
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

        // Cache service (scoped)
        services.AddScoped<IInventoryCacheService, InventoryCacheService>();

        // Idempotency service (scoped)
        services.AddScoped<IIdempotencyService>(sp =>
            new RedisIdempotencyService(sp.GetRequiredService<IConnectionMultiplexer>(), "idempotency:inv:"));

        // Distributed lock (singleton — thread-safe, just wraps the multiplexer)
        services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();

        // Event producer (singleton — Kafka producer is thread-safe)
        services.AddSingleton<IInventoryEventProducer, InventoryEventProducer>();

        // Background consumers (hosted services)
        services.AddHostedService<ProductCreatedConsumer>();
        services.AddHostedService<CartCheckedOutConsumer>();
        services.AddHostedService<CartCancelledConsumer>();

        return services;
    }

    public static async Task MigrateInventoryDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await db.Database.MigrateAsync();
    }
}
