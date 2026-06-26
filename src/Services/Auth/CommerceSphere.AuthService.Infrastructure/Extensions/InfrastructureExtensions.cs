using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.AuthService.Infrastructure.Data;
using CommerceSphere.AuthService.Infrastructure.Kafka.Producers;
using CommerceSphere.AuthService.Infrastructure.Redis;
using CommerceSphere.AuthService.Infrastructure.Services;
using CommerceSphere.AuthService.Infrastructure.UnitOfWork;
using CommerceSphere.Shared.Common.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CommerceSphere.AuthService.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AuthDbContext>(opts =>
            // EnableRetryOnFailure handles transient Postgres connection drops (e.g. container restart)
            // without requiring manual retry logic in the application layer.
            opts.UseNpgsql(config.GetConnectionString("AuthDb"),
                npg => npg.EnableRetryOnFailure(3)));

        var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
        // IConnectionMultiplexer is a singleton because StackExchange.Redis manages an internal
        // connection pool — creating one per-request would thrash connections.
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IIdempotencyService, RedisIdempotencyService>();
        services.AddSingleton<IUserEventProducer, UserEventProducer>();

        return services;
    }

    // Migrations are applied at startup rather than by a separate CLI step so that
    // Docker containers self-migrate on first run without manual intervention.
    public static async Task MigrateAuthDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.Database.MigrateAsync();
    }
}
