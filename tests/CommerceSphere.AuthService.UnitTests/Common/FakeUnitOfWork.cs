using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.AuthService.Domain.Interfaces.Repositories;

namespace CommerceSphere.AuthService.UnitTests.Common;

// In-memory IUnitOfWork so manager tests exercise real read-after-write behaviour
// (register → user exists, refresh-token rotation, session revocation) without a database.
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public FakeUserRepository UsersStore { get; } = new();
    public FakeRefreshTokenRepository RefreshTokensStore { get; } = new();
    public FakeRoleRepository RolesStore { get; } = new();
    public FakeMenuRepository MenusStore { get; } = new();
    public FakeRoleMenuPermissionRepository PermissionsStore { get; } = new();

    public IUserRepository Users => UsersStore;
    public IRefreshTokenRepository RefreshTokens => RefreshTokensStore;
    public IRoleRepository Roles => RolesStore;
    public IMenuRepository Menus => MenusStore;
    public IRoleMenuPermissionRepository Permissions => PermissionsStore;

    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }

    public Task BeginTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task CommitTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task RollbackTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void Dispose() { }
}

public sealed class FakeUserRepository : IUserRepository
{
    public List<User> Items { get; } = [];
    private readonly List<(string Provider, string ExternalId, Guid UserId)> _externalLogins = [];

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(Items.Any(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        Items.Add(user);
        return Task.CompletedTask;
    }

    // No-op: the in-memory instance is already the tracked reference.
    public void Update(User user) { }

    public void Remove(User user) => Items.Remove(user);

    public Task<int> CountByRoleAsync(string role, CancellationToken ct = default)
        => Task.FromResult(Items.Count(u => string.Equals(u.Role, role, StringComparison.OrdinalIgnoreCase)));

    public Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var page = Items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IEnumerable<User>)page, Items.Count));
    }

    public Task<User?> GetByExternalLoginAsync(string provider, string externalUserId, CancellationToken ct = default)
    {
        var link = _externalLogins.FirstOrDefault(l => l.Provider == provider && l.ExternalId == externalUserId);
        return Task.FromResult(link.UserId == Guid.Empty ? null : Items.FirstOrDefault(u => u.Id == link.UserId));
    }

    public Task AddExternalLoginAsync(ExternalLogin login, CancellationToken ct = default)
    {
        _externalLogins.Add((login.Provider, login.ExternalUserId, login.UserId));
        return Task.CompletedTask;
    }

    public Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(u => u.EmailVerificationToken == token));

    public Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(u => u.PasswordResetToken == token));
}

public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Items { get; } = [];

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(t => t.Token == token));

    public Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        Items.Add(token);
        return Task.CompletedTask;
    }

    public void Update(RefreshToken token) { }

    public Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        foreach (var t in Items.Where(t => t.UserId == userId && t.IsActive))
            t.Revoke();
        return Task.CompletedTask;
    }
}

public sealed class FakeRoleRepository : IRoleRepository
{
    public List<Role> Items { get; } = [];

    public Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Role>)Items.ToList());

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(r => r.Id == id));

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct = default)
        => Task.FromResult(Items.Any(r =>
            string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase) && (excludeId == null || r.Id != excludeId)));

    public Task AddAsync(Role role, CancellationToken ct = default)
    {
        Items.Add(role);
        return Task.CompletedTask;
    }

    public void Update(Role role) { }
    public void Remove(Role role) => Items.Remove(role);
}

public sealed class FakeMenuRepository : IMenuRepository
{
    public List<Menu> Items { get; } = [];

    public Task<IReadOnlyList<Menu>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Menu>)Items.ToList());

    public Task<Menu?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(m => m.Id == id));

    public Task<bool> ExistsByKeyAsync(string key, Guid? excludeId, CancellationToken ct = default)
        => Task.FromResult(Items.Any(m =>
            string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase) && (excludeId == null || m.Id != excludeId)));

    public Task AddAsync(Menu menu, CancellationToken ct = default)
    {
        Items.Add(menu);
        return Task.CompletedTask;
    }

    public void Update(Menu menu) { }
    public void Remove(Menu menu) => Items.Remove(menu);
}

public sealed class FakeRoleMenuPermissionRepository : IRoleMenuPermissionRepository
{
    public List<RoleMenuPermission> Items { get; } = [];

    public Task<List<RoleMenuPermission>> GetByRoleIdAsync(Guid roleId, CancellationToken ct = default)
        => Task.FromResult(Items.Where(p => p.RoleId == roleId).ToList());

    public Task<IReadOnlyList<RoleMenuPermission>> GetByRoleNameAsync(string roleName, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<RoleMenuPermission>)[]);

    public Task AddAsync(RoleMenuPermission permission, CancellationToken ct = default)
    {
        Items.Add(permission);
        return Task.CompletedTask;
    }
}
