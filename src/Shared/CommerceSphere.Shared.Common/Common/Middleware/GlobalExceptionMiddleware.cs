using System.Net;
using System.Text.Json;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Exceptions;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.Shared.Common.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.GetCorrelationId();
        var traceId = context.TraceIdentifier;

        // Map every known domain exception to an HTTP status code in one place so individual
        // controllers never need try/catch blocks — they can throw and this middleware handles it.
        var (statusCode, message, errors) = exception switch
        {
            ValidationException vex   => (HttpStatusCode.BadRequest,        "Validation failed",        vex.Errors),
            NotFoundException nex     => (HttpStatusCode.NotFound,           nex.Message,                Enumerable.Empty<string>()),
            UnauthorizedException uex => (HttpStatusCode.Unauthorized,       uex.Message,                Enumerable.Empty<string>()),
            BusinessException bex     => (HttpStatusCode.UnprocessableEntity, bex.Message,               Enumerable.Empty<string>()),
            ConflictException cex     => (HttpStatusCode.Conflict,           cex.Message,                Enumerable.Empty<string>()),
            ConcurrencyException ccex => (HttpStatusCode.Conflict,           ccex.Message,               Enumerable.Empty<string>()),
            IdempotencyException iex  => (HttpStatusCode.Conflict,           iex.Message,                Enumerable.Empty<string>()),
            // SSO flow errors (expired state token, bad code, provider rejection) → 400 so the
            // client knows it must restart the login flow rather than retrying the same request.
            SsoException sex          => (HttpStatusCode.BadRequest,         sex.Message,                Enumerable.Empty<string>()),
            // Unknown exception → 500; message is hidden from the client to avoid leaking internals.
            _                         => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", Enumerable.Empty<string>())
        };

        // Log 500s as Error (pages on-call); log known domain exceptions as Warning (expected, not alarming).
        if (statusCode == HttpStatusCode.InternalServerError)
            logger.LogError(exception, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
        else
            logger.LogWarning(exception, "Handled exception {ExType}. CorrelationId: {CorrelationId}", exception.GetType().Name, correlationId);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.Fail(message, errors, traceId, correlationId);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
