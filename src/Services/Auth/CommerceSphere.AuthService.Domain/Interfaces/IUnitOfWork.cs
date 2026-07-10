using CommerceSphere.AuthService.Domain.Interfaces.Repositories;

namespace CommerceSphere.AuthService.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IRoleRepository Roles { get; }
    IMenuRepository Menus { get; }
    IRoleMenuPermissionRepository Permissions { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    // Runs the given work inside a database transaction, wrapped in the provider's retrying
    // execution strategy so it is compatible with EnableRetryOnFailure (a plain user-initiated
    // BeginTransaction throws under that strategy). The action is retried as a unit on transient
    // failures and rolled back automatically if it throws.
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
}
