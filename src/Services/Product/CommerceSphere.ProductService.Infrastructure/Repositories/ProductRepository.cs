using CommerceSphere.ProductService.Domain.Entities;
using CommerceSphere.ProductService.Domain.Interfaces.Repositories;
using CommerceSphere.ProductService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.ProductService.Infrastructure.Repositories;

public class ProductRepository(ProductDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == sku.Trim().ToUpperInvariant(), ct);

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default) =>
        db.Products.AnyAsync(p => p.Sku == sku.Trim().ToUpperInvariant(), ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await db.Products.AddAsync(product, ct);

    public void Update(Product product) =>
        db.Products.Update(product);

    public async Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? category,
        string? searchTerm,
        bool publishedOnly,
        decimal? maxPrice,
        bool inStockOnly,
        string? sortBy,
        CancellationToken ct = default)
    {
        var query = db.Products.AsNoTracking().AsQueryable();

        if (publishedOnly)
            query = query.Where(p => p.IsActive && p.IsPublished);

        if (!string.IsNullOrWhiteSpace(category))
        {
            // Accept one name or a comma-separated list (parent category + its children).
            var names = category
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (names.Length == 1)
                query = query.Where(p => p.Category == names[0]);
            else if (names.Length > 1)
                query = query.Where(p => names.Contains(p.Category));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Description.ToLower().Contains(term) ||
                p.Sku.ToLower().Contains(term));
        }

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        if (inStockOnly)
            query = query.Where(p => p.Stock > 0);

        // Always append a stable tiebreaker (Id) so paging never duplicates/skips rows when
        // many products share a sort key — essential for correct infinite scroll.
        query = sortBy switch
        {
            "price-asc" => query.OrderBy(p => p.Price).ThenBy(p => p.Id),
            "price-desc" => query.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            "name" => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
            _ => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
        };

        var total = await query.CountAsync(ct);
        var products = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (products, total);
    }
}
