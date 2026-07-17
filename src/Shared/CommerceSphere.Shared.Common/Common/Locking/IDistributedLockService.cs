namespace CommerceSphere.Shared.Common.Locking;

public interface IDistributedLockService
{
    /// <summary>
    /// Attempts to acquire an exclusive lock on <paramref name="resource"/>, retrying until
    /// <paramref name="wait"/> elapses. The lock auto-expires after <paramref name="expiry"/> even
    /// if never released, so a crashed holder can never wedge the resource forever.
    /// </summary>
    /// <returns>A handle that releases the lock on DisposeAsync, or null if it could not be acquired in time.</returns>
    Task<IAsyncDisposable?> AcquireAsync(
        string resource, TimeSpan expiry, TimeSpan wait, CancellationToken ct = default);
}
