using CommerceSphere.ProductService.Domain.Interfaces;
using CommerceSphere.ProductService.Domain.Interfaces.Repositories;
using CommerceSphere.ProductService.Infrastructure.Data;
using CommerceSphere.ProductService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace CommerceSphere.ProductService.Infrastructure.UnitOfWork;

public class UnitOfWork(ProductDbContext db) : IUnitOfWork
{
    private IProductRepository? _products;
    private ICategoryRepository? _categories;
    private IBannerRepository? _banners;
    private IBulkImportJobRepository? _bulkImportJobs;
    private IDbContextTransaction? _transaction;

    public IProductRepository Products => _products ??= new ProductRepository(db);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(db);
    public IBannerRepository Banners => _banners ??= new BannerRepository(db);
    public IBulkImportJobRepository BulkImportJobs => _bulkImportJobs ??= new BulkImportJobRepository(db);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default) =>
        _transaction = await db.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            await _transaction.CommitAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            await _transaction.RollbackAsync(ct);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        db.Dispose();
    }
}
