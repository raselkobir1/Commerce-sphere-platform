namespace CommerceSphere.Shared.Common.Models;

// Standard envelope for every API response. Including TraceId and CorrelationId lets clients
// give engineers both IDs when reporting a bug, making log lookups trivial across services.
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];
    public string TraceId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;

    public static ApiResponse<T> Ok(T data, string message = "Success", string traceId = "", string correlationId = "") =>
        new() { Success = true, Message = message, Data = data, TraceId = traceId, CorrelationId = correlationId };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null, string traceId = "", string correlationId = "") =>
        new() { Success = false, Message = message, Errors = errors ?? [], TraceId = traceId, CorrelationId = correlationId };
}

// Non-generic variant for responses that have no payload (e.g., logout, revoke-token).
// Inheriting from ApiResponse<object> reuses the same JSON structure without duplicating fields.
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "Success", string traceId = "", string correlationId = "") =>
        new() { Success = true, Message = message, TraceId = traceId, CorrelationId = correlationId };

    public new static ApiResponse Fail(string message, IEnumerable<string>? errors = null, string traceId = "", string correlationId = "") =>
        new() { Success = false, Message = message, Errors = errors ?? [], TraceId = traceId, CorrelationId = correlationId };
}
