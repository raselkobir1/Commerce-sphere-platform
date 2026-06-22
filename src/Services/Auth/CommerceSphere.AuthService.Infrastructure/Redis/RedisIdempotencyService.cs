using CommerceSphere.Shared.Common.Idempotency;
using StackExchange.Redis;

namespace CommerceSphere.AuthService.Infrastructure.Redis;

public class RedisIdempotencyService(IConnectionMultiplexer redis) : IIdempotencyService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<bool> IsProcessedAsync(string key, CancellationToken ct = default) =>
        await _db.KeyExistsAsync($"idempotency:{key}");

    public async Task MarkProcessedAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default) =>
        await _db.StringSetAsync($"idempotency:{key}", "1", expiry ?? TimeSpan.FromHours(24));
}
