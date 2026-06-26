using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace CommerceSphere.AuthService.IntegrationTests.Infrastructure;

// Boots the real Auth API pipeline (controllers, middleware, EF Core, JWT auth) against
// throwaway PostgreSQL + Redis containers. Only the external edges — SMTP, Kafka, Keycloak —
// are faked, so the request → manager → repository → database path is exercised for real.
public sealed class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("auth_test")
        .WithUsername("commerce")
        .WithPassword("commerce_pass")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public FakeEmailService Email { get; } = new();
    public FakeUserEventProducer Events { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        // Set as environment variables (not ConfigureAppConfiguration) so they are present when
        // Program.Main reads builder.Configuration — the minimal-hosting builder is constructed
        // before the factory's ConfigureAppConfiguration callbacks would otherwise run.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__AuthDb", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", _redis.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Secret", "Integration_Test_Super_Secret_JWT_Key_2024_Min32Chars!!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "CommerceSphere");
        Environment.SetEnvironmentVariable("Jwt__Audience", "CommerceSphereClients");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
        // Blank authority → KeycloakOptions.Validate() is skipped; the service itself is faked.
        Environment.SetEnvironmentVariable("Keycloak__Authority", "");
        Environment.SetEnvironmentVariable("Keycloak__ClientSecret", "test-secret");
        Environment.SetEnvironmentVariable("Kafka__BootstrapServers", "localhost:9092");
        Environment.SetEnvironmentVariable("Email__SmtpHost", "localhost");
        Environment.SetEnvironmentVariable("Email__AppBaseUrl", "http://localhost");

        // Build the schema from the current EF model rather than the migrations. The committed
        // migration files lack the [Migration]/[DbContext] designer attributes, so EF's scanner
        // recognises none of them (the running stack is provisioned via raw SQL instead). The
        // Testcontainers database already exists, so EnsureCreated would no-op — we create the
        // tables directly, which always matches the model including every security column.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var creator = (RelationalDatabaseCreator)db.GetService<IDatabaseCreator>();
        await creator.CreateTablesAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Email);

            services.RemoveAll<IUserEventProducer>();
            services.AddSingleton<IUserEventProducer>(Events);

            services.RemoveAll<IKeycloakService>();
            services.AddSingleton<IKeycloakService, FakeKeycloakService>();
        });
    }
}

[CollectionDefinition(Name)]
public sealed class AuthApiCollection : ICollectionFixture<AuthApiFactory>
{
    public const string Name = "AuthApi";
}
