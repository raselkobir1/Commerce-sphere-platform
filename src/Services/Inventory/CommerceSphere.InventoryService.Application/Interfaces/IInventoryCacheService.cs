using CommerceSphere.InventoryService.Application.DTOs.Responses;

namespace CommerceSphere.InventoryService.Application.Interfaces;

public interface IInventoryCacheService
{
    Task<InventoryItemResponse?> GetInventoryAsync(Guid productId, CancellationToken ct = default);
    Task SetInventoryAsync(Guid productId, InventoryItemResponse item, CancellationToken ct = default);
    Task RemoveInventoryAsync(Guid productId, CancellationToken ct = default);
}
