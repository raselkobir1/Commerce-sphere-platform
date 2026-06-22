using System.Text.Json;
using CommerceSphere.ProductService.Application.DTOs.Responses;
using CommerceSphere.ProductService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CommerceSphere.ProductService.Infrastructure.Redis;

public class ProductCacheService(IConnectionMultiplexer redis, ILogger<ProductCacheService> logger) : IProductCacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private static string CacheKey(Guid id) => $"product:{id}";

    public async Task<ProductResponse?> GetProductAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var value = await _db.StringGetAsync(CacheKey(id));
            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<ProductResponse>(value!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis GET failed for ProductId: {ProductId}. Falling back to database.", id);
            return null;
        }
    }

    public async Task SetProductAsync(ProductResponse product, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(product);
            await _db.StringSetAsync(CacheKey(product.Id), json, DefaultTtl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis SET failed for ProductId: {ProductId}.", product.Id);
        }
    }

    public async Task RemoveProductAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _db.KeyDeleteAsync(CacheKey(id));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis DELETE failed for ProductId: {ProductId}.", id);
        }
    }
}
