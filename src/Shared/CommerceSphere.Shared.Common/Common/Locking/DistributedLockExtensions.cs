namespace CommerceSphere.Shared.Common.Locking;

public static class DistributedLockExtensions
{
    /// <summary>
    /// Acquires locks on every distinct resource, always in a stable sorted order, so two callers
    /// locking the same set of resources can never deadlock on each other.
    /// </summary>
    /// <returns>A handle releasing all acquired locks, or null if any single lock could not be acquired in time
    /// (in which case every lock already taken for this call is released before returning).</returns>
    public static async Task<IAsyncDisposable?> AcquireAllAsync(
        this IDistributedLockService locks,
        IEnumerable<string> resources,
        TimeSpan expiry,
        TimeSpan wait,
        CancellationToken ct = default)
    {
        var ordered = resources.Distinct().OrderBy(r => r, StringComparer.Ordinal).ToList();
        var acquired = new List<IAsyncDisposable>(ordered.Count);

        foreach (var resource in ordered)
        {
            var handle = await locks.AcquireAsync(resource, expiry, wait, ct);
            if (handle is null)
            {
                foreach (var held in acquired)
                    await held.DisposeAsync();
                return null;
            }
            acquired.Add(handle);
        }

        return new CompositeLockHandle(acquired);
    }

    private sealed class CompositeLockHandle(IReadOnlyList<IAsyncDisposable> handles) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            foreach (var handle in handles)
                await handle.DisposeAsync();
        }
    }
}
