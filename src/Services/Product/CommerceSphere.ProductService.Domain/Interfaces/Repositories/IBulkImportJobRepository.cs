using CommerceSphere.ProductService.Domain.Entities;

namespace CommerceSphere.ProductService.Domain.Interfaces.Repositories;

public interface IBulkImportJobRepository
{
    Task<BulkImportJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(BulkImportJob job, CancellationToken ct = default);
    void Update(BulkImportJob job);
}
