namespace CommerceSphere.Shared.Common.Idempotency;

public interface IIdempotencyService
{
    Task<bool> IsProcessedAsync(string key, CancellationToken ct = default);
    Task MarkProcessedAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// Atomically checks-and-claims <paramref name="key"/> in a single round trip (Redis SETNX),
    /// closing the check-then-mark race that a separate IsProcessedAsync + MarkProcessedAsync pair leaves open.
    /// </summary>
    /// <returns>true if this call newly claimed the key; false if it was already claimed/processed.</returns>
    Task<bool> TryMarkProcessedAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default);
}
