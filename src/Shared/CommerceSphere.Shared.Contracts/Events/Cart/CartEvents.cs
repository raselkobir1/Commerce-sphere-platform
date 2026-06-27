namespace CommerceSphere.Shared.Contracts.Events.Cart;

public record CartCreatedEvent(
    Guid CartId,
    Guid UserId,
    string CorrelationId,
    DateTime OccurredAt,
    int Version = 1
);

public record CartUpdatedEvent(
    Guid CartId,
    Guid UserId,
    int ItemCount,
    decimal TotalAmount,
    string CorrelationId,
    DateTime OccurredAt,
    int Version = 1
);

public record CartCheckedOutEvent(
    Guid CartId,
    Guid UserId,
    decimal TotalAmount,
    IReadOnlyList<CartItemSnapshot> Items,
    string CorrelationId,
    DateTime OccurredAt,
    int Version = 1
);

public record CartRolledBackEvent(
    Guid CartId,
    Guid UserId,
    string Reason,
    string CorrelationId,
    DateTime OccurredAt,
    int Version = 1
);

// Raised when an admin cancels a placed order — drives stock restock + a customer email.
public record CartCancelledEvent(
    Guid CartId,
    Guid UserId,
    decimal TotalAmount,
    IReadOnlyList<CartItemSnapshot> Items,
    string Reason,
    string CorrelationId,
    DateTime OccurredAt,
    int Version = 1
);

public record CartItemSnapshot(Guid ProductId, string Sku, string ProductName, int Quantity, decimal UnitPrice);
