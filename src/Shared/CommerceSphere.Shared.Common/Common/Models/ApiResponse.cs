namespace CommerceSphere.Shared.Common.Models;

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

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "Success", string traceId = "", string correlationId = "") =>
        new() { Success = true, Message = message, TraceId = traceId, CorrelationId = correlationId };

    public new static ApiResponse Fail(string message, IEnumerable<string>? errors = null, string traceId = "", string correlationId = "") =>
        new() { Success = false, Message = message, Errors = errors ?? [], TraceId = traceId, CorrelationId = correlationId };
}
