using CommerceSphere.CartService.Domain.Entities;

namespace CommerceSphere.CartService.Domain.Interfaces.Repositories;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Cart?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);
    Task AddAsync(Cart cart, CancellationToken ct = default);
    void Update(Cart cart);
    Task<(IEnumerable<Cart> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);
    // Orders = carts that have been checked out, newest first.
    Task<IReadOnlyList<Cart>> GetOrdersAsync(CancellationToken ct = default);
}
