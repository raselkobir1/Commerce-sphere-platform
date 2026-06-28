using CommerceSphere.NotificationService.Application.Handlers;
using CommerceSphere.NotificationService.Application.Interfaces;
using CommerceSphere.NotificationService.Application.Managers;
using CommerceSphere.NotificationService.Domain.Interfaces;
using CommerceSphere.NotificationService.Infrastructure.Data;
using CommerceSphere.NotificationService.Infrastructure.Email;
using CommerceSphere.NotificationService.Infrastructure.Kafka.Consumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceSphere.NotificationService.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<NotificationDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("NotificationDb"),
                npg => npg.EnableRetryOnFailure(3)));

        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<INotificationManager, NotificationManager>();
        services.AddScoped<IOrderEventHandler, OrderEventHandler>();

        services.Configure<EmailOptions>(config.GetSection("Email"));
        services.AddScoped<IEmailSender, EmailSender>();

        // The Kafka consumer that drives everything.
        services.AddHostedService<NotificationConsumer>();

        return services;
    }

    public static async Task MigrateNotificationDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        await db.Database.MigrateAsync();
    }
}
