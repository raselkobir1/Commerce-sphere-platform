using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.AuthService.Infrastructure.Data;
using CommerceSphere.AuthService.Infrastructure.Email;
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
            opts.UseNpgsql(config.GetConnectionString("AuthDb"),
                npg => npg.EnableRetryOnFailure(3)));

        var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IIdempotencyService, RedisIdempotencyService>();
        services.AddSingleton<IUserEventProducer, UserEventProducer>();

        // Security services
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<IOtpCodeService, OtpCodeService>();
        services.AddSingleton<IChallengeTokenService, ChallengeTokenService>();

        // Email
        services.Configure<EmailOptions>(config.GetSection("Email"));
        services.AddScoped<IEmailService, EmailService>();

        // --- Keycloak / SSO ---
        var keycloakSection = config.GetSection("Keycloak");
        services.Configure<KeycloakOptions>(keycloakSection);

        var keycloakOpts = keycloakSection.Get<KeycloakOptions>();
        if (keycloakOpts is not null && !string.IsNullOrWhiteSpace(keycloakOpts.Authority))
            keycloakOpts.Validate();

        services.AddHttpClient<IKeycloakService, KeycloakService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }

    public static async Task MigrateAuthDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.Database.MigrateAsync();
    }
}
