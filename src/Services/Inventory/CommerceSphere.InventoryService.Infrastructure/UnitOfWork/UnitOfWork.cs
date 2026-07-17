using CommerceSphere.InventoryService.Domain.Interfaces;
using CommerceSphere.InventoryService.Domain.Interfaces.Repositories;
using CommerceSphere.InventoryService.Infrastructure.Data;
using CommerceSphere.InventoryService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.InventoryService.Infrastructure.UnitOfWork;

public class UnitOfWork(InventoryDbContext db) : IUnitOfWork
{
    private IInventoryRepository? _inventory;
    private IReservationRepository? _reservations;

    public IInventoryRepository Inventory => _inventory ??= new InventoryRepository(db);
    public IReservationRepository Reservations => _reservations ??= new ReservationRepository(db);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation();
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    public void Dispose() => db.Dispose();
}
