namespace CommerceSphere.Shared.Common.Exceptions;

// Thrown when a business rule is violated (e.g. checking out an empty cart).
// Maps to HTTP 422 Unprocessable Entity in GlobalExceptionMiddleware.
public class BusinessException(string message) : Exception(message);

// Thrown when a requested resource does not exist. Maps to HTTP 404.
public class NotFoundException(string entityName, object key)
    : Exception($"{entityName} with key '{key}' was not found.");

// Thrown by FluentValidation validators via the global middleware. Maps to HTTP 400.
// Carries the full list of field-level error messages so clients can display them inline.
public class ValidationException(IEnumerable<string> errors)
    : Exception("One or more validation errors occurred.")
{
    public IEnumerable<string> Errors { get; } = errors;
}

// Thrown for authentication failures (bad credentials, expired token). Maps to HTTP 401.
public class UnauthorizedException(string message = "Unauthorized access.")
    : Exception(message);

// Thrown when creating a resource that already exists (e.g., duplicate email or SKU). Maps to HTTP 409.
public class ConflictException(string message) : Exception(message);

// Thrown by EF Core optimistic concurrency: two concurrent requests tried to update the same row.
// Maps to HTTP 409 — the client should refresh and retry.
public class ConcurrencyException(string message = "The record was modified by another user. Please refresh and try again.")
    : Exception(message);

// Thrown when a duplicate idempotency key is detected. Maps to HTTP 409.
// The caller already got a successful response for this key — they should use that result.
public class IdempotencyException(string key)
    : Exception($"Request with idempotency key '{key}' was already processed.");
