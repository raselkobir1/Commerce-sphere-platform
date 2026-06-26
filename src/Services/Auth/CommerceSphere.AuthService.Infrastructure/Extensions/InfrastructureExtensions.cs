using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.AuthService.Infrastructure.Data;
using CommerceSphere.AuthService.Infrastructure.Kafka.Producers;
using CommerceSphere.AuthService.Infrastructure.Keycloak;
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

        // --- Keycloak / SSO ---
        // Bind and eagerly validate Keycloak options so a missing ClientSecret or Authority
        // causes a clear startup error rather than a confusing runtime failure at login time.
        var keycloakSection = config.GetSection("Keycloak");
        services.Configure<KeycloakOptions>(keycloakSection);

        var keycloakOpts = keycloakSection.Get<KeycloakOptions>();
        if (keycloakOpts is not null)
        {
            // Only validate when the section is present and a ClientSecret has been changed
            // from the placeholder — allows running without SSO in envs that don't need it.
            if (!string.IsNullOrWhiteSpace(keycloakOpts.Authority))
                keycloakOpts.Validate();
        }

        // Named HttpClient for Keycloak — 15-second timeout because the code exchange
        // is user-visible latency; failing fast is better than a hanging login screen.
        services.AddHttpClient<IKeycloakService, KeycloakService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

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
