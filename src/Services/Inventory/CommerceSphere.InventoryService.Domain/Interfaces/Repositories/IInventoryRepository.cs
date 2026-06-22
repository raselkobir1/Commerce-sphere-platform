using CommerceSphere.InventoryService.Domain.Entities;

namespace CommerceSphere.InventoryService.Domain.Interfaces.Repositories;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<InventoryItem?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task AddAsync(InventoryItem item, CancellationToken ct = default);
    void Update(InventoryItem item);
    Task<(IEnumerable<InventoryItem> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken ct = default);
}
