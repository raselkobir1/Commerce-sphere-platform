namespace CommerceSphere.Shared.Contracts.Events.Auth;

public record UserCreatedEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime OccurredAt,
    string CorrelationId,
    int Version = 1
);
