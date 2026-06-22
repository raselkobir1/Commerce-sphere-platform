namespace CommerceSphere.InventoryService.Application.DTOs.Requests;

public record ReserveInventoryRequest(
    Guid CartId,
    Guid UserId,
    IReadOnlyList<ReserveItemRequest> Items,
    string IdempotencyKey
);

public record ReserveItemRequest(
    Guid ProductId,
    string Sku,
    int Quantity,
    decimal UnitPrice
);

public record ReleaseReservationRequest(
    Guid ReservationId,
    Guid CartId,
    string Reason
);

public record AdjustStockRequest(
    Guid ProductId,
    string Sku,
    int NewQuantity
);

public record ReceiveStockRequest(
    Guid ProductId,
    string Sku,
    int Quantity
);
