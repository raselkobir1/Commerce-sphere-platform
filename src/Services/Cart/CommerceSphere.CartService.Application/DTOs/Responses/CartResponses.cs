namespace CommerceSphere.CartService.Application.DTOs.Responses;

public record CartItemResponse(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    DateTime AddedAt);

public record CartResponse(
    Guid Id,
    Guid UserId,
    string Status,
    IEnumerable<CartItemResponse> Items,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
