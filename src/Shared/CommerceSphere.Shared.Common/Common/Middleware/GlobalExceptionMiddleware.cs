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

        var (statusCode, message, errors) = exception switch
        {
            ValidationException vex   => (HttpStatusCode.BadRequest,        "Validation failed",        vex.Errors),
            NotFoundException nex     => (HttpStatusCode.NotFound,           nex.Message,                Enumerable.Empty<string>()),
            UnauthorizedException uex => (HttpStatusCode.Unauthorized,       uex.Message,                Enumerable.Empty<string>()),
            BusinessException bex     => (HttpStatusCode.UnprocessableEntity, bex.Message,               Enumerable.Empty<string>()),
            ConflictException cex     => (HttpStatusCode.Conflict,           cex.Message,                Enumerable.Empty<string>()),
            ConcurrencyException cex  => (HttpStatusCode.Conflict,           cex.Message,                Enumerable.Empty<string>()),
            IdempotencyException iex  => (HttpStatusCode.Conflict,           iex.Message,                Enumerable.Empty<string>()),
            _                         => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", Enumerable.Empty<string>())
        };

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
