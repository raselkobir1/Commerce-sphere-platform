using CommerceSphere.InventoryService.Domain.Interfaces.Repositories;

namespace CommerceSphere.InventoryService.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IInventoryRepository Inventory { get; }
    IReservationRepository Reservations { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
