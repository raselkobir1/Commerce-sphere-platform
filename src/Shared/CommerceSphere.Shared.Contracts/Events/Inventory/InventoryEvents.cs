namespace CommerceSphere.Shared.Contracts.Events.Inventory;

public record InventoryReservedEvent(
    Guid ReservationId,
    Guid CartId,
    Guid UserId,
    IReadOnlyList<ReservedItem> Items,
    string CorrelationId,
    DateTime OccurredAt,
    int Version = 1
);

public record InventoryReservationFailedEvent(
    Guid CartId,
    Guid UserId,
    string Reason,
    string CorrelationId,
    DateTime OccurredAt,
    int Version = 1
);

public record InventoryReleasedEvent(
    Guid ReservationId,
    Guid CartId,
    string Reason,
    string CorrelationId,
    DateTime OccurredAt,
    int Version = 1
);

public record ReservedItem(Guid ProductId, string Sku, int Quantity, decimal UnitPrice);
