namespace CommerceSphere.Shared.Common.Exceptions;

public class BusinessException(string message) : Exception(message);

public class NotFoundException(string entityName, object key)
    : Exception($"{entityName} with key '{key}' was not found.");

public class ValidationException(IEnumerable<string> errors)
    : Exception("One or more validation errors occurred.")
{
    public IEnumerable<string> Errors { get; } = errors;
}

public class UnauthorizedException(string message = "Unauthorized access.")
    : Exception(message);

public class ConflictException(string message) : Exception(message);

public class ConcurrencyException(string message = "The record was modified by another user. Please refresh and try again.")
    : Exception(message);

public class IdempotencyException(string key)
    : Exception($"Request with idempotency key '{key}' was already processed.");
