using CommerceSphere.CartService.Domain.Interfaces.Repositories;

namespace CommerceSphere.CartService.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ICartRepository Carts { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
