using CommerceSphere.Shared.Common.Idempotency;
using StackExchange.Redis;

namespace CommerceSphere.InventoryService.Infrastructure.Redis;

public class RedisIdempotencyService(IConnectionMultiplexer redis) : IIdempotencyService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string KeyPrefix = "idempotency:inv:";

    public async Task<bool> IsProcessedAsync(string key, CancellationToken ct = default) =>
        await _db.KeyExistsAsync($"{KeyPrefix}{key}");

    public async Task MarkProcessedAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default) =>
        await _db.StringSetAsync($"{KeyPrefix}{key}", "1", expiry ?? TimeSpan.FromHours(24));
}
