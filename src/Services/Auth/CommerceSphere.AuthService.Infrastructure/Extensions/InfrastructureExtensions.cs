using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Entities;
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
using AuthRole = CommerceSphere.AuthService.Domain.Entities.Role;

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

    // Seeds the default RBAC data on startup (idempotent):
    //   - system roles Admin + Customer (Customer = default for new sign-ups)
    //   - the admin navigation menus
    //   - full CRUD permissions on every menu for the Admin role
    public static async Task SeedRbacAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        if (!await db.Roles.AnyAsync(r => r.Name == "Admin"))
            await db.Roles.AddAsync(AuthRole.Create("Admin", "Full access to every menu and action", isSystem: true));

        if (!await db.Roles.AnyAsync(r => r.Name == "Customer"))
            await db.Roles.AddAsync(AuthRole.Create("Customer", "Storefront shopper (no admin access)", isSystem: true, isDefault: true));

        if (!await db.Menus.AnyAsync())
        {
            await db.Menus.AddRangeAsync(
                Menu.Create("dashboard", "Dashboard", "/dashboard", "📊", 1),
                Menu.Create("products", "Products", "/products", "🛍️", 2),
                Menu.Create("categories", "Categories", "/categories", "🏷️", 3),
                Menu.Create("inventory", "Inventory", "/inventory", "📦", 4),
                Menu.Create("users", "Users", "/users", "👥", 5),
                Menu.Create("roles", "Roles", "/roles", "🛡️", 6),
                Menu.Create("menus", "Menus", "/menus", "🧭", 7),
                Menu.Create("permissions", "Permissions", "/permissions", "🔐", 8),
                Menu.Create("settings", "Settings", "/settings", "⚙️", 9));
        }

        await db.SaveChangesAsync();

        // Ensure the Admin role has full CRUD on every menu (also covers menus added later).
        var admin = await db.Roles.FirstAsync(r => r.Name == "Admin");
        var grantedMenuIds = await db.RolePermissions
            .Where(p => p.RoleId == admin.Id).Select(p => p.MenuId).ToListAsync();
        var missing = await db.Menus.Where(m => !grantedMenuIds.Contains(m.Id)).ToListAsync();
        foreach (var m in missing)
            await db.RolePermissions.AddAsync(RoleMenuPermission.Create(admin.Id, m.Id, true, true, true, true));
        if (missing.Count > 0)
            await db.SaveChangesAsync();
    }
}
