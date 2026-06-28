using CommerceSphere.CartService.Domain.Interfaces;
using CommerceSphere.CartService.Domain.Interfaces.Repositories;
using CommerceSphere.CartService.Infrastructure.Data;
using CommerceSphere.CartService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace CommerceSphere.CartService.Infrastructure.UnitOfWork;

public class UnitOfWork(CartDbContext context) : IUnitOfWork
{
    private ICartRepository? _carts;
    private INotificationRepository? _notifications;
    private IDbContextTransaction? _transaction;

    public ICartRepository Carts => _carts ??= new CartRepository(context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }
}
