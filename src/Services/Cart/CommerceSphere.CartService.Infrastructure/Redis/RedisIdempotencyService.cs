using CommerceSphere.Shared.Common.Idempotency;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CommerceSphere.CartService.Infrastructure.Redis;

public class RedisIdempotencyService(IConnectionMultiplexer redis, ILogger<RedisIdempotencyService> logger) : IIdempotencyService
{
    private const string KeyPrefix = "idempotency:cart:";
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(24);

    private static string BuildKey(string key) => $"{KeyPrefix}{key}";

    public async Task<bool> IsProcessedAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            return await db.KeyExistsAsync(BuildKey(key));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis idempotency check failed for key {Key}", key);
            return false;
        }
    }

    public async Task MarkProcessedAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.StringSetAsync(BuildKey(key), "1", expiry ?? DefaultExpiry);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis idempotency mark failed for key {Key}", key);
        }
    }
}
