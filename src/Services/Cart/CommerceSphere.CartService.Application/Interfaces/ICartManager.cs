using CommerceSphere.CartService.Application.DTOs.Requests;
using CommerceSphere.CartService.Application.DTOs.Responses;

namespace CommerceSphere.CartService.Application.Interfaces;

public interface ICartManager
{
    Task<CartResponse> CreateCartAsync(CreateCartRequest request, string correlationId, CancellationToken ct = default);
    Task<CartResponse> AddItemAsync(Guid cartId, AddCartItemRequest request, string correlationId, CancellationToken ct = default);
    Task<CartResponse> UpdateItemAsync(Guid cartId, UpdateCartItemRequest request, CancellationToken ct = default);
    Task<CartResponse> RemoveItemAsync(Guid cartId, Guid productId, CancellationToken ct = default);
    Task<CartResponse> GetCartAsync(Guid cartId, CancellationToken ct = default);
    Task<CartResponse> GetCartByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<CartResponse>> GetOrdersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CartResponse>> GetUserOrdersAsync(Guid userId, CancellationToken ct = default);
    Task<CartResponse> ConfirmOrderAsync(Guid cartId, CancellationToken ct = default);
    Task<CartResponse> ShipOrderAsync(Guid cartId, CancellationToken ct = default);
    Task<CartResponse> CancelOrderAsync(Guid cartId, string reason, string correlationId, CancellationToken ct = default);
    // Customer-initiated cancel — only succeeds if the order belongs to userId.
    Task<CartResponse> CancelOwnOrderAsync(Guid cartId, Guid userId, string reason, string correlationId, CancellationToken ct = default);
    Task<CartResponse> CheckoutAsync(CheckoutCartRequest request, string correlationId, CancellationToken ct = default);
    Task RollbackAsync(Guid cartId, string reason, CancellationToken ct = default);
}
