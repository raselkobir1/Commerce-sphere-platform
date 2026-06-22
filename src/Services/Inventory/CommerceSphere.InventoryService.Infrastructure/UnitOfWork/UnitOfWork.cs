using CommerceSphere.InventoryService.Domain.Interfaces;
using CommerceSphere.InventoryService.Domain.Interfaces.Repositories;
using CommerceSphere.InventoryService.Infrastructure.Data;
using CommerceSphere.InventoryService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace CommerceSphere.InventoryService.Infrastructure.UnitOfWork;

public class UnitOfWork(InventoryDbContext db) : IUnitOfWork
{
    private IInventoryRepository? _inventory;
    private IReservationRepository? _reservations;
    private IDbContextTransaction? _transaction;

    public IInventoryRepository Inventory => _inventory ??= new InventoryRepository(db);
    public IReservationRepository Reservations => _reservations ??= new ReservationRepository(db);

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
