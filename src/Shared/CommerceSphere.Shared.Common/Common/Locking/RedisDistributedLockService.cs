using StackExchange.Redis;

namespace CommerceSphere.Shared.Common.Locking;

public class RedisDistributedLockService(IConnectionMultiplexer redis) : IDistributedLockService
{
    private const string KeyPrefix = "lock:";
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(100);

    // Only delete the key if it still holds the token we set: if our lease expired and someone
    // else already acquired the lock, this must not delete their (unrelated) lock.
    private const string ReleaseScript = """
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end
        """;

    public async Task<IAsyncDisposable?> AcquireAsync(
        string resource, TimeSpan expiry, TimeSpan wait, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var key = $"{KeyPrefix}{resource}";
        var token = Guid.NewGuid().ToString("N");
        var deadline = DateTime.UtcNow + wait;

        while (true)
        {
            if (await db.StringSetAsync(key, token, expiry, When.NotExists))
                return new LockHandle(db, key, token);

            if (DateTime.UtcNow >= deadline)
                return null;

            await Task.Delay(RetryInterval, ct);
        }
    }

    private sealed class LockHandle(IDatabase db, string key, string token) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() =>
            await db.ScriptEvaluateAsync(ReleaseScript, [(RedisKey)key], [(RedisValue)token]);
    }
}
