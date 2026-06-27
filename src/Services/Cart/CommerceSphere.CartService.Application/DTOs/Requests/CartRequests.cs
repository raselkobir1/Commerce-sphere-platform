namespace CommerceSphere.CartService.Application.DTOs.Requests;

public record CreateCartRequest(Guid UserId, string? IdempotencyKey);

public record AddCartItemRequest(
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record UpdateCartItemRequest(Guid ProductId, int Quantity);

public record RemoveCartItemRequest(Guid ProductId);

public record CheckoutCartRequest(Guid CartId, Guid UserId);

public record CancelOrderRequest(string? Reason);
