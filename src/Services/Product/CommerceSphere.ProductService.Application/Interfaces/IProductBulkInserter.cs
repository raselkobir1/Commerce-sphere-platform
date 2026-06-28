using CommerceSphere.ProductService.Domain.Entities;

namespace CommerceSphere.ProductService.Application.Interfaces;

// High-throughput persistence for bulk imports. The implementation uses PostgreSQL COPY
// (binary import), which bypasses the EF change tracker — orders of magnitude faster than
// AddRange + SaveChanges at 100K rows. Callers must pre-filter duplicate SKUs (the unique
// index is the final guard, but COPY aborts the whole batch on conflict).
public interface IProductBulkInserter
{
    // Returns the subset of the given SKUs that already exist, so callers can skip them.
    Task<HashSet<string>> GetExistingSkusAsync(IReadOnlyCollection<string> skus, CancellationToken ct = default);

    // Streams the products into the table via COPY.
    Task BulkInsertAsync(IReadOnlyCollection<Product> products, CancellationToken ct = default);
}
