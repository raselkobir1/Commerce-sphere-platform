using CommerceSphere.CartService.Application.DTOs.Responses;

namespace CommerceSphere.CartService.Application.Interfaces;

public interface ICartCacheService
{
    Task<CartResponse?> GetCartAsync(Guid cartId);
    Task SetCartAsync(Guid cartId, CartResponse cart);
    Task RemoveCartAsync(Guid cartId);
}
