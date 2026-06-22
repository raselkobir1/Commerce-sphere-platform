using CommerceSphere.CartService.Domain.Entities;
using CommerceSphere.CartService.Domain.Interfaces.Repositories;
using CommerceSphere.CartService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.CartService.Infrastructure.Repositories;

public class CartRepository(CartDbContext context) : ICartRepository
{
    public async Task<Cart?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Carts
            .Include(c => c.Items)
            .Where(c => c.UserId == userId && c.Status == CartStatus.Active)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Cart?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
    {
        return await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.IdempotencyKey == key, ct);
    }

    public async Task AddAsync(Cart cart, CancellationToken ct = default)
    {
        await context.Carts.AddAsync(cart, ct);
    }

    public void Update(Cart cart)
    {
        context.Carts.Update(cart);
    }

    public async Task<(IEnumerable<Cart> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
