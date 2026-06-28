using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.ProductService.Domain.Interfaces;
using CommerceSphere.ProductService.Infrastructure.Data;
using CommerceSphere.ProductService.Infrastructure.Kafka.Producers;
using CommerceSphere.ProductService.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CommerceSphere.ProductService.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ProductDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("ProductDb"),
                npg => npg.EnableRetryOnFailure(3)));

        var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<IProductCacheService, ProductCacheService>();
        services.AddSingleton<IProductEventProducer, ProductEventProducer>();

        // Bulk product import (Excel upload → async background processing via PostgreSQL COPY).
        services.AddSingleton<IExcelProductParser, Excel.ClosedXmlProductParser>();
        services.AddSingleton<IBulkImportFileStore, BulkImport.BulkImportFileStore>();
        services.AddSingleton<IBulkImportQueue, BulkImport.BulkImportQueue>();
        services.AddScoped<IProductBulkInserter, BulkImport.NpgsqlProductBulkInserter>();
        services.AddHostedService<BulkImport.BulkImportBackgroundWorker>();

        // Keep catalog Stock in sync when carts check out / are cancelled.
        services.AddHostedService<Kafka.Consumers.CartCheckedOutConsumer>();
        services.AddHostedService<Kafka.Consumers.CartCancelledConsumer>();

        return services;
    }

    public static async Task MigrateProductDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await db.Database.MigrateAsync();
    }
}
