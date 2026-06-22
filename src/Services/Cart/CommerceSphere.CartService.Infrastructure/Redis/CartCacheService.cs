using System.Text.Json;
using CommerceSphere.CartService.Application.DTOs.Responses;
using CommerceSphere.CartService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CommerceSphere.CartService.Infrastructure.Redis;

public class CartCacheService(IConnectionMultiplexer redis, ILogger<CartCacheService> logger) : ICartCacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);
    private static string CacheKey(Guid cartId) => $"cart:{cartId}";

    public async Task<CartResponse?> GetCartAsync(Guid cartId)
    {
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync(CacheKey(cartId));
            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<CartResponse>(value!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis GET failed for cart {CartId}. Falling back to database.", cartId);
            return null;
        }
    }

    public async Task SetCartAsync(Guid cartId, CartResponse cart)
    {
        try
        {
            var db = redis.GetDatabase();
            var json = JsonSerializer.Serialize(cart);
            await db.StringSetAsync(CacheKey(cartId), json, DefaultTtl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis SET failed for cart {CartId}. Proceeding without cache.", cartId);
        }
    }

    public async Task RemoveCartAsync(Guid cartId)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(CacheKey(cartId));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis DELETE failed for cart {CartId}.", cartId);
        }
    }
}
