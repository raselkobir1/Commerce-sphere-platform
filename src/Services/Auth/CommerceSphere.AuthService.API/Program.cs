using System.Text;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Application.Managers;
using CommerceSphere.AuthService.Application.Validators;
using AccountManager = CommerceSphere.AuthService.Application.Managers.AccountManager;
using TwoFactorManager = CommerceSphere.AuthService.Application.Managers.TwoFactorManager;
using OtpManager = CommerceSphere.AuthService.Application.Managers.OtpManager;
using CommerceSphere.AuthService.Infrastructure.Keycloak;
using CommerceSphere.AuthService.Infrastructure.Extensions;
using CommerceSphere.Shared.Common.Extensions;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "AuthService")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/auth-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            // Pin the signing algorithm so a token can't be accepted under a different alg
            // (defence-in-depth against algorithm-confusion attacks).
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            // ClockSkew defaults to 5 minutes — zero enforces the exact expiry we set in JwtService.
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opts.AddPolicy("CustomerOrAdmin", p => p.RequireRole("Customer", "Admin"));
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<AuthManager>();  // concrete type needed by TwoFactorManager + OtpManager
builder.Services.AddScoped<IAccountManager, AccountManager>();
builder.Services.AddScoped<IRbacManager, RbacManager>();
builder.Services.AddScoped<ITwoFactorManager, TwoFactorManager>();
builder.Services.AddScoped<IOtpManager, OtpManager>();

// Register the SSO manager — coordinates Keycloak token exchange and local user creation.
builder.Services.AddScoped<ISsoManager, SsoManager>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("AuthService"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CommerceSphere Auth API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "Bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, [] }
    });
});

var app = builder.Build();

app.UseGlobalExceptionHandler();
app.UseCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

// Apply pending EF Core migrations automatically on startup so Docker containers
// are always in sync with the latest schema without a manual migration step.
await app.Services.MigrateAuthDbAsync();
await app.Services.SeedRbacAsync();

app.Run();

// Exposed so the integration test project can host the real pipeline via
// WebApplicationFactory<Program>. Top-level statements otherwise emit an internal Program.
public partial class Program { }
