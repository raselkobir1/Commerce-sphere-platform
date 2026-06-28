using CommerceSphere.ProductService.Domain.Entities;
using CommerceSphere.ProductService.Domain.Interfaces.Repositories;
using CommerceSphere.ProductService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.ProductService.Infrastructure.Repositories;

public class BulkImportJobRepository(ProductDbContext db) : IBulkImportJobRepository
{
    public Task<BulkImportJob?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.BulkImportJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task AddAsync(BulkImportJob job, CancellationToken ct = default) =>
        await db.BulkImportJobs.AddAsync(job, ct);

    public void Update(BulkImportJob job) =>
        db.BulkImportJobs.Update(job);
}
