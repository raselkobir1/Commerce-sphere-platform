using CommerceSphere.Shared.Common.Idempotency;
using StackExchange.Redis;

namespace CommerceSphere.AuthService.Infrastructure.Redis;

public class RedisIdempotencyService(IConnectionMultiplexer redis) : IIdempotencyService
{
    private readonly IDatabase _db = redis.GetDatabase();

    // The "idempotency:" prefix namespaces keys so they don't collide with other Redis data.
    public async Task<bool> IsProcessedAsync(string key, CancellationToken ct = default) =>
        await _db.KeyExistsAsync($"idempotency:{key}");

    // 24-hour default TTL: long enough to cover retries from any realistic client, short enough
    // that Redis doesn't accumulate stale keys indefinitely.
    public async Task MarkProcessedAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default) =>
        await _db.StringSetAsync($"idempotency:{key}", "1", expiry ?? TimeSpan.FromHours(24));
}
