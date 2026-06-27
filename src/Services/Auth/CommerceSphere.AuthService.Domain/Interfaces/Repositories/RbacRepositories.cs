using CommerceSphere.AuthService.Domain.Entities;

namespace CommerceSphere.AuthService.Domain.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct = default);
    Task AddAsync(Role role, CancellationToken ct = default);
    void Update(Role role);
    void Remove(Role role);
}

public interface IMenuRepository
{
    Task<IReadOnlyList<Menu>> GetAllAsync(CancellationToken ct = default);
    Task<Menu?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByKeyAsync(string key, Guid? excludeId, CancellationToken ct = default);
    Task AddAsync(Menu menu, CancellationToken ct = default);
    void Update(Menu menu);
    void Remove(Menu menu);
}

public interface IRoleMenuPermissionRepository
{
    // Tracked (for upsert) permissions of a role.
    Task<List<RoleMenuPermission>> GetByRoleIdAsync(Guid roleId, CancellationToken ct = default);
    // Read-only permissions for a role looked up by its name, with the Menu eager-loaded (for /me).
    Task<IReadOnlyList<RoleMenuPermission>> GetByRoleNameAsync(string roleName, CancellationToken ct = default);
    Task AddAsync(RoleMenuPermission permission, CancellationToken ct = default);
}
