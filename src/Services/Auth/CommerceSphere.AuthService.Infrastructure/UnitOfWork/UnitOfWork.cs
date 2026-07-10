using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.AuthService.Domain.Interfaces.Repositories;
using CommerceSphere.AuthService.Infrastructure.Data;
using CommerceSphere.AuthService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.AuthService.Infrastructure.UnitOfWork;

public class UnitOfWork(AuthDbContext db) : IUnitOfWork
{
    private IUserRepository? _users;
    private IRefreshTokenRepository? _refreshTokens;
    private IRoleRepository? _roles;
    private IMenuRepository? _menus;
    private IRoleMenuPermissionRepository? _permissions;

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

    // EnableRetryOnFailure installs a retrying execution strategy, which forbids a plain
    // user-initiated BeginTransaction. The strategy must own the transaction so it can retry the
    // whole unit on a transient failure; it also rolls back automatically if the action throws.
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await action();
            await transaction.CommitAsync(ct);
        });
    }

    public void Dispose()
    {
        // No-op: the DI container owns AuthDbContext (scoped lifetime) and disposes it when the
        // request scope ends. Transactions are scoped to ExecuteInTransactionAsync and disposed there.
    }
}
