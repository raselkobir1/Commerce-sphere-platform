using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CommerceSphere.Shared.Common.Resilience;

public static class ResiliencePolicies
{
    // Only retries on transient failures (timeouts, connection drops) — not on domain errors
    // like constraint violations, which would fail on every retry.
    public static AsyncRetryPolicy DatabaseRetryPolicy(ILogger logger) =>
        Policy
            .Handle<Exception>(ex => IsTransient(ex))
            // Exponential back-off: 2s, 4s, 8s — gives the DB time to recover without flooding it.
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, attempt, _) =>
                    logger.LogWarning(ex, "DB transient failure. Retry {Attempt} in {Delay}ms", attempt, delay.TotalMilliseconds));

    // Kafka producers can encounter transient leader elections or broker restarts; 5 retries
    // handle most short outages without losing events.
    public static AsyncRetryPolicy KafkaRetryPolicy(ILogger logger) =>
        Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, attempt, _) =>
                    logger.LogWarning(ex, "Kafka publish failure. Retry {Attempt} in {Delay}ms", attempt, delay.TotalMilliseconds));

    public static AsyncRetryPolicy HttpRetryPolicy(ILogger logger) =>
        Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt),
                onRetry: (ex, delay, attempt, _) =>
                    logger.LogWarning(ex, "HTTP call failure. Retry {Attempt} in {Delay}ms", attempt, delay.TotalMilliseconds));

    // Classify an exception as transient (safe to retry) based on its type or message text.
    private static bool IsTransient(Exception ex) =>
        ex is TimeoutException or InvalidOperationException ||
        ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("transient", StringComparison.OrdinalIgnoreCase);
}
