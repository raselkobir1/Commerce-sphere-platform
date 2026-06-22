using CommerceSphere.InventoryService.Domain.Entities;
using CommerceSphere.InventoryService.Domain.Interfaces.Repositories;
using CommerceSphere.InventoryService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.InventoryService.Infrastructure.Repositories;

public class InventoryRepository(InventoryDbContext db) : IInventoryRepository
{
    public Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default) =>
        db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId, ct);

    public Task<InventoryItem?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        db.InventoryItems.FirstOrDefaultAsync(
            i => i.Sku == sku.Trim().ToUpperInvariant(), ct);

    public async Task AddAsync(InventoryItem item, CancellationToken ct = default) =>
        await db.InventoryItems.AddAsync(item, ct);

    public void Update(InventoryItem item) =>
        db.InventoryItems.Update(item);

    public async Task<(IEnumerable<InventoryItem> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = db.InventoryItems.AsNoTracking().OrderBy(i => i.Sku);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
