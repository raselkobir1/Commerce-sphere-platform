using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.Shared.Common.Correlation;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        // Accept a correlation ID from the caller (e.g., API Gateway forwards it) so the same
        // ID flows through every service in the chain. Generate a new one if none is provided.
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Store in HttpContext.Items so downstream code can retrieve it via GetCorrelationId().
        context.Items[HeaderName] = correlationId;
        // Echo back in the response so clients can match their request to logs.
        context.Response.Headers[HeaderName] = correlationId;

        // BeginScope attaches the correlation ID to every log entry written during this request,
        // enabling log aggregation tools to filter all logs for a single request in one query.
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}

public static class CorrelationIdExtensions
{
    public static string GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var id)
            ? id?.ToString() ?? string.Empty
            : string.Empty;
}
