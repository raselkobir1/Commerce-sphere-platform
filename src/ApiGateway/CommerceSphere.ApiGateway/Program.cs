using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CommerceSphere.Shared.Common.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "ApiGateway")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/gateway-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            // ClockSkew defaults to 5 minutes, which would silently accept tokens up to 5 min
            // past their stated expiry. Zero enforces the exact expiry time we set in JwtService.
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opts.AddPolicy("CustomerOrAdmin", p => p.RequireRole("Customer", "Admin"));
});

builder.Services.AddRateLimiter(opts =>
{
    // 200 requests per minute per fixed window; up to 20 additional requests can queue
    // rather than being rejected immediately, smoothing short traffic spikes.
    opts.AddFixedWindowLimiter("gateway-fixed", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 200;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 20;
    });
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("ApiGateway"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseGlobalExceptionHandler();
app.UseCorrelationId();
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId",
            httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? string.Empty);
        diagnosticContext.Set("RemoteIp",
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    };
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy(pipeline =>
{
    pipeline.Use(async (context, next) =>
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(correlationId))
            context.Response.Headers["X-Correlation-Id"] = correlationId;

        // Forward the authenticated user's identity as trusted HTTP headers so downstream
        // services can access UserId/Role without re-validating the JWT themselves.
        // These headers are only set by the gateway (after JWT validation) — downstream
        // services should reject any request where these headers come from outside.
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? context.User.FindFirst("sub")?.Value;

            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
            var userName = context.User.FindFirst(ClaimTypes.Name)?.Value;

            if (userId != null)
                context.Request.Headers["X-User-Id"] = userId;

            if (userRole != null)
                context.Request.Headers["X-User-Role"] = userRole;

            if (userName != null)
                context.Request.Headers["X-User-Name"] = userName;
        }
        await next();
    });
});

app.Run();
