using StackExchange.Redis;

namespace CommerceSphere.Shared.Common.Idempotency;

// keyPrefix namespaces keys per service so they don't collide with each other or with other Redis
// data — all services share one Redis instance (see docker-compose.yml).
public class RedisIdempotencyService(IConnectionMultiplexer redis, string keyPrefix) : IIdempotencyService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(24);

    private readonly IDatabase _db = redis.GetDatabase();

    private string BuildKey(string key) => $"{keyPrefix}{key}";

    public async Task<bool> IsProcessedAsync(string key, CancellationToken ct = default) =>
        await _db.KeyExistsAsync(BuildKey(key));

    public async Task MarkProcessedAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default) =>
        await _db.StringSetAsync(BuildKey(key), "1", expiry ?? DefaultExpiry);

    public async Task<bool> TryMarkProcessedAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default) =>
        await _db.StringSetAsync(BuildKey(key), "1", expiry ?? DefaultExpiry, When.NotExists);
}
