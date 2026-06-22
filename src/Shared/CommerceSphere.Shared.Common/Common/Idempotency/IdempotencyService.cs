namespace CommerceSphere.Shared.Common.Idempotency;

public interface IIdempotencyService
{
    Task<bool> IsProcessedAsync(string key, CancellationToken ct = default);
    Task MarkProcessedAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default);
}
