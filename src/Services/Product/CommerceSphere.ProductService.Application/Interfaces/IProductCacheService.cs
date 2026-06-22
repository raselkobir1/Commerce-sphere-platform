using CommerceSphere.ProductService.Application.DTOs.Responses;

namespace CommerceSphere.ProductService.Application.Interfaces;

public interface IProductCacheService
{
    Task<ProductResponse?> GetProductAsync(Guid id, CancellationToken ct = default);
    Task SetProductAsync(ProductResponse product, CancellationToken ct = default);
    Task RemoveProductAsync(Guid id, CancellationToken ct = default);
}
