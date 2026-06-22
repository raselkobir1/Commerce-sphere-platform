namespace CommerceSphere.Shared.Contracts.Events.Product;

public record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    string Sku,
    decimal Price,
    int InitialStock,
    string CorrelationId,
    DateTime OccurredAt,
    int Version = 1
);

public record ProductUpdatedEvent(
    Guid ProductId,
    string Name,
    decimal Price,
    bool IsActive,
    string CorrelationId,
    DateTime OccurredAt,
    int Version = 1
);
