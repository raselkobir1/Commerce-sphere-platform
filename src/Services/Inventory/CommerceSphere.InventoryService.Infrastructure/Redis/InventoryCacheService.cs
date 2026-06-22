using System.Text.Json;
using CommerceSphere.InventoryService.Application.DTOs.Responses;
using CommerceSphere.InventoryService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CommerceSphere.InventoryService.Infrastructure.Redis;

public class InventoryCacheService(IConnectionMultiplexer redis, ILogger<InventoryCacheService> logger)
    : IInventoryCacheService
{
    private readonly IDatabase _db = redis.GetDatabase();

    // Inventory changes frequently — use 1 minute TTL
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(1);

    private static string CacheKey(Guid productId) => $"inventory:{productId}";

    public async Task<InventoryItemResponse?> GetInventoryAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var value = await _db.StringGetAsync(CacheKey(productId));
            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<InventoryItemResponse>(value!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Redis GET failed for ProductId: {ProductId}. Falling back to database.", productId);
            return null;
        }
    }

    public async Task SetInventoryAsync(Guid productId, InventoryItemResponse item, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(item);
            await _db.StringSetAsync(CacheKey(productId), json, DefaultTtl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis SET failed for ProductId: {ProductId}.", productId);
        }
    }

    public async Task RemoveInventoryAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            await _db.KeyDeleteAsync(CacheKey(productId));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis DELETE failed for ProductId: {ProductId}.", productId);
        }
    }
}
