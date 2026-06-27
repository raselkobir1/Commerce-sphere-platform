using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.AuthService.Domain.Interfaces.Repositories;
using CommerceSphere.AuthService.Infrastructure.Data;
using CommerceSphere.AuthService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace CommerceSphere.AuthService.Infrastructure.UnitOfWork;

public class UnitOfWork(AuthDbContext db) : IUnitOfWork
{
    private IUserRepository? _users;
    private IRefreshTokenRepository? _refreshTokens;
    private IRoleRepository? _roles;
    private IMenuRepository? _menus;
    private IRoleMenuPermissionRepository? _permissions;
    private IDbContextTransaction? _transaction;

    // Repositories are created lazily so we don't pay allocation cost for repos that
    // a given use-case never touches.
    public IUserRepository Users => _users ??= new UserRepository(db);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(db);
    public IRoleRepository Roles => _roles ??= new RoleRepository(db);
    public IMenuRepository Menus => _menus ??= new MenuRepository(db);
    public IRoleMenuPermissionRepository Permissions => _permissions ??= new RoleMenuPermissionRepository(db);

    // Single SaveChanges call writes all tracked changes in one round-trip, making the
    // entire use-case operation atomic from the database perspective.
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
        // Only dispose the transaction — the DI container owns AuthDbContext (scoped lifetime)
        // and disposes it when the request scope ends. Disposing it here causes double-dispose.
        _transaction?.Dispose();
    }
}
