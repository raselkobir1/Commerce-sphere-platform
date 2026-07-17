using CommerceSphere.InventoryService.Domain.Interfaces.Repositories;

namespace CommerceSphere.InventoryService.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IInventoryRepository Inventory { get; }
    IReservationRepository Reservations { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a DB transaction via EF Core's configured execution
    /// strategy (required for EnableRetryOnFailure — a bare BeginTransaction/Commit pair throws the
    /// moment a query runs inside it). Commits on success, rolls back and rethrows on failure.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
}
