using CommerceSphere.ProductService.Domain.Entities;

namespace CommerceSphere.ProductService.Domain.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    void Update(Product product);
    Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? category,
        string? searchTerm,
        bool publishedOnly,
        decimal? maxPrice,
        bool inStockOnly,
        string? sortBy,
        CancellationToken ct = default);
}
