namespace CommerceSphere.InventoryService.Application.DTOs.Responses;

public record InventoryItemResponse(
    Guid Id,
    Guid ProductId,
    string Sku,
    int QuantityOnHand,
    int QuantityReserved,
    int QuantityAvailable,
    int ReorderLevel,
    bool IsActive
);

public record ReservationResponse(
    Guid Id,
    Guid CartId,
    Guid UserId,
    string Status,
    IReadOnlyList<ReservationItemResponse> Items,
    DateTime CreatedAt
);

public record ReservationItemResponse(
    Guid ProductId,
    string Sku,
    int Quantity,
    decimal UnitPrice
);
